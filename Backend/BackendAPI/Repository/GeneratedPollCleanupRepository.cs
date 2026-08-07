using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository;

public sealed class GeneratedPollCleanupRepository(DapperContext context) : IGeneratedPollCleanupRepository
{
    private sealed class CandidateRow : GeneratedPollCleanupCandidate
    {
        public long? OptionId { get; set; }
        public string? OptionText { get; set; }
        public string? OptionSide { get; set; }
    }

    public async Task<IReadOnlyList<GeneratedPollCleanupCandidate>> GetCandidatesAsync(long fromPollId, long toPollId, int maxRecords)
    {
        using var connection = context.CreateConnection();
        var rows = await connection.QueryAsync<CandidateRow>(@"
            WITH candidates AS (
                SELECT TOP (@MaxRecords) p.Id
                FROM Polls p
                WHERE p.IsAIGenerated = 1 AND p.Id BETWEEN @FromPollId AND @ToPollId
                ORDER BY p.Id
            )
            SELECT p.Id PollId, p.Question, p.IsAIGenerated, p.IsActive, p.IsTrending,
                   p.SourceType, p.SourceUrl,
                   (SELECT COUNT_BIG(1) FROM Votes v WHERE v.PollId=p.Id) VoteCount,
                   q.TrendingTopicId, q.GenerationProvider, c.Status CleanupStatus,
                   o.Id OptionId, o.Text OptionText, o.Side OptionSide
            FROM candidates x JOIN Polls p ON p.Id=x.Id
            LEFT JOIN GeneratedPollQualityDecisions q ON q.PollId=p.Id
            LEFT JOIN GeneratedPollCleanupRecords c ON c.PollId=p.Id
            LEFT JOIN PollOptions o ON o.PollId=p.Id
            ORDER BY p.Id, o.Id", new { FromPollId = fromPollId, ToPollId = toPollId, MaxRecords = maxRecords });
        return rows.GroupBy(x => x.PollId).Select(g =>
        {
            var first = g.First();
            first.Options = g.Where(x => x.OptionId.HasValue).Select(x => new PollOption
                { Id = x.OptionId!.Value, PollId = x.PollId, Text = x.OptionText ?? string.Empty, Side = x.OptionSide }).ToList();
            return (GeneratedPollCleanupCandidate)first;
        }).ToArray();
    }

    public async Task<CleanupApplyResult> ApplyAsync(long pollId, Guid runId, string detectionVersion,
        IReadOnlyList<string> reasons, string generationSource)
    {
        using var connection = context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var poll = await connection.QuerySingleOrDefaultAsync<(bool IsAIGenerated, bool IsActive)>(
                "SELECT IsAIGenerated, IsActive FROM Polls WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE Id=@PollId",
                new { PollId = pollId }, transaction);
            if (!poll.IsAIGenerated) throw new InvalidOperationException("Poll is missing or is not AI-generated.");
            var votes = await connection.ExecuteScalarAsync<long>(
                "SELECT COUNT_BIG(1) FROM Votes WITH (UPDLOCK, HOLDLOCK) WHERE PollId=@PollId", new { PollId = pollId }, transaction);
            var disposition = votes == 0 ? GeneratedPollCleanupPolicy.DeactivateAndRegenerate : GeneratedPollCleanupPolicy.PreserveAndHide;
            var existing = await connection.QuerySingleOrDefaultAsync<(long Id, string Status, string Disposition)>(
                "SELECT Id, Status, Disposition FROM GeneratedPollCleanupRecords WITH (UPDLOCK, HOLDLOCK) WHERE PollId=@PollId",
                new { PollId = pollId }, transaction);
            if (existing.Id != 0 && existing.Status == "Completed")
            {
                transaction.Commit();
                return new(false, existing.Disposition, existing.Status);
            }
            long cleanupId;
            if (existing.Id == 0)
            {
                cleanupId = await connection.ExecuteScalarAsync<long>(@"
                    INSERT INTO GeneratedPollCleanupRecords
                      (PollId, TrendingTopicId, DetectionVersion, ReasonCode, GenerationSource, VoteCountAtCleanup,
                       Disposition, Status, DetectedAt, CleanedAt, LastAttemptAt, AttemptCount, RunId)
                    OUTPUT inserted.Id
                    SELECT @PollId, q.TrendingTopicId, @DetectionVersion, @ReasonCode, @GenerationSource, @Votes,
                           @Disposition, 'Deactivated', SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(), 1, @RunId
                    FROM (VALUES(1)) x(n) LEFT JOIN GeneratedPollQualityDecisions q ON q.PollId=@PollId",
                    new { PollId = pollId, DetectionVersion = detectionVersion, ReasonCode = string.Join(',', reasons),
                        GenerationSource = generationSource, Votes = votes, Disposition = disposition, RunId = runId }, transaction);
            }
            else
            {
                cleanupId = existing.Id;
                await connection.ExecuteAsync(@"
                    UPDATE GeneratedPollCleanupRecords SET VoteCountAtCleanup=@Votes, Disposition=@Disposition,
                      Status=CASE WHEN @Disposition='PreserveAndHide' THEN 'Completed' ELSE Status END,
                      CleanedAt=COALESCE(CleanedAt,SYSUTCDATETIME()), LastAttemptAt=SYSUTCDATETIME(),
                      AttemptCount=AttemptCount+1, RunId=@RunId, LastError=NULL WHERE Id=@Id",
                    new { Votes = votes, Disposition = disposition, RunId = runId, Id = cleanupId }, transaction);
            }
            await connection.ExecuteAsync("UPDATE Polls SET IsActive=0, IsTrending=0 WHERE Id=@PollId AND (IsActive=1 OR IsTrending=1)",
                new { PollId = pollId }, transaction);
            var status = "Completed";
            if (disposition == GeneratedPollCleanupPolicy.DeactivateAndRegenerate)
            {
                await connection.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM GeneratedPollRegenerationQueue WITH (UPDLOCK,HOLDLOCK) WHERE CleanupRecordId=@CleanupId)
                      INSERT INTO GeneratedPollRegenerationQueue(CleanupRecordId,Status,AvailableAt,AttemptCount,CreatedAt,UpdatedAt)
                      VALUES(@CleanupId,'Queued',SYSUTCDATETIME(),0,SYSUTCDATETIME(),SYSUTCDATETIME())",
                    new { CleanupId = cleanupId }, transaction);
                status = "RegenerationQueued";
            }
            await connection.ExecuteAsync("UPDATE GeneratedPollCleanupRecords SET Status=@Status WHERE Id=@Id",
                new { Status = status, Id = cleanupId }, transaction);
            transaction.Commit();
            return new(poll.IsActive || existing.Id == 0, disposition, status);
        }
        catch { transaction.Rollback(); throw; }
    }

    public async Task<IReadOnlyList<RegenerationQueueItem>> ClaimRegenerationBatchAsync(int maxRecords)
    {
        if (maxRecords is <= 0 or > GeneratedPollCleanupPolicy.MaximumBatchSize) throw new ArgumentOutOfRangeException(nameof(maxRecords));
        using var connection = context.CreateConnection(); connection.Open();
        using var transaction = connection.BeginTransaction();
        var claimed = (await connection.QueryAsync<(long CleanupRecordId, int AttemptCount)>(@"
            ;WITH claim AS (SELECT TOP (@MaxRecords) * FROM GeneratedPollRegenerationQueue WITH (UPDLOCK,READPAST,ROWLOCK)
              WHERE Status IN ('Queued','Failed') AND AvailableAt<=SYSUTCDATETIME()
                AND (LeaseExpiresAt IS NULL OR LeaseExpiresAt<SYSUTCDATETIME()) ORDER BY Id)
            UPDATE claim SET Status='Processing', LeaseExpiresAt=DATEADD(MINUTE,10,SYSUTCDATETIME()),
              AttemptCount=AttemptCount+1, UpdatedAt=SYSUTCDATETIME()
            OUTPUT inserted.CleanupRecordId, inserted.AttemptCount;", new { MaxRecords = maxRecords }, transaction)).ToArray();
        if (claimed.Length == 0) { transaction.Commit(); return []; }
        var attempts = claimed.ToDictionary(x => x.CleanupRecordId, x => x.AttemptCount);
        await connection.ExecuteAsync(@"UPDATE GeneratedPollCleanupRecords
            SET Status='Regenerating',LastAttemptAt=SYSUTCDATETIME(),AttemptCount=AttemptCount+1
            WHERE Id IN @Ids AND ReplacementPollId IS NULL", new { Ids = claimed.Select(x => x.CleanupRecordId).ToArray() }, transaction);
        var items = (await connection.QueryAsync<RegenerationQueueItem>(@"
            SELECT c.Id CleanupRecordId, c.PollId, c.TrendingTopicId, p.SourceUrl, 0 AttemptCount,
                   replacement.Id ReplacementPollId
            FROM GeneratedPollCleanupRecords c
            JOIN Polls p ON p.Id=c.PollId
            LEFT JOIN Polls replacement ON replacement.ReplacementForCleanupRecordId=c.Id
            WHERE c.Id IN @Ids",
            new { Ids = claimed.Select(x => x.CleanupRecordId).ToArray() }, transaction)).ToArray();
        transaction.Commit();
        return items.Select(x => x with { AttemptCount = attempts[x.CleanupRecordId] }).ToArray();
    }

    public async Task<TrendingTopic?> ResolveTopicAsync(RegenerationQueueItem item)
    {
        using var connection = context.CreateConnection();
        if (item.TrendingTopicId.HasValue)
            return await connection.QuerySingleOrDefaultAsync<TrendingTopic>("SELECT * FROM TrendingTopics WHERE Id=@Id", new { Id = item.TrendingTopicId });
        if (string.IsNullOrWhiteSpace(item.SourceUrl)) return null;
        var matches = (await connection.QueryAsync<TrendingTopic>("SELECT * FROM TrendingTopics WHERE SourceUrl=@SourceUrl", new { item.SourceUrl })).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    public async Task CompleteRegenerationAsync(RegenerationQueueItem item, long replacementPollId)
    {
        using var connection = context.CreateConnection(); connection.Open(); using var tx = connection.BeginTransaction();
        await connection.ExecuteAsync("UPDATE GeneratedPollCleanupRecords SET ReplacementPollId=COALESCE(ReplacementPollId,@ReplacementPollId),Status='Completed',LastError=NULL WHERE Id=@Id AND (ReplacementPollId IS NULL OR ReplacementPollId=@ReplacementPollId)",
            new { ReplacementPollId = replacementPollId, Id = item.CleanupRecordId }, tx);
        await connection.ExecuteAsync("UPDATE GeneratedPollRegenerationQueue SET Status='Completed',LeaseExpiresAt=NULL,LastError=NULL,UpdatedAt=SYSUTCDATETIME() WHERE CleanupRecordId=@Id",
            new { Id = item.CleanupRecordId }, tx); tx.Commit();
    }

    public async Task FailRegenerationAsync(RegenerationQueueItem item, string error)
    {
        var safe = error.Length > 2000 ? error[..2000] : error;
        using var connection = context.CreateConnection();
        await connection.ExecuteAsync(@"UPDATE GeneratedPollRegenerationQueue SET Status='Failed',AvailableAt=DATEADD(MINUTE,5,SYSUTCDATETIME()),LeaseExpiresAt=NULL,LastError=@Error,UpdatedAt=SYSUTCDATETIME() WHERE CleanupRecordId=@Id;
            UPDATE GeneratedPollCleanupRecords SET Status='Failed',LastError=@Error,LastAttemptAt=SYSUTCDATETIME() WHERE Id=@Id;",
            new { Error = safe, Id = item.CleanupRecordId });
    }
}
