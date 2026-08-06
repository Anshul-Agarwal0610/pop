using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class TrendingTopicRepository : ITrendingTopicRepository
    {
        private readonly DapperContext _context;

        public TrendingTopicRepository(DapperContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Insert topics that don't already exist (dedup by SourceUrl).
        /// Empty or null SourceUrls are always inserted.
        /// </summary>
        public async Task<TopicSaveResult> SaveBatchAsync(IEnumerable<TrendingTopic> topics, string? correlationId = null)
        {
            var batch = topics.ToList();
            if (batch.Count == 0) return new(0, 0, 0);

            using var conn = _context.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            var inserted = 0;

            foreach (var topic in batch)
            {
                var normalizedCategory = CategoryCatalog.NormalizeName(topic.Category);

                inserted += await conn.ExecuteAsync(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM TrendingTopics
                        WHERE (SourceUrl = @SourceUrl AND SourceUrl <> '')
                           OR (LOWER(LTRIM(RTRIM(Title))) = LOWER(LTRIM(RTRIM(@Title)))
                               AND FetchedAt >= DATEADD(HOUR, -48, GETUTCDATE()))
                    )
                    BEGIN
                        INSERT INTO TrendingTopics
                            (Title, Summary, SourceType, SourceUrl, ThumbnailUrl, Publisher, PublishedAt, Category, FetchedAt, IsProcessed, ProcessingStatus, CorrelationId)
                        VALUES
                            (@Title, @Summary, @SourceType, @SourceUrl, @ThumbnailUrl, @Publisher, @PublishedAt, @Category, GETUTCDATE(), 0, 'Queued', @CorrelationId)
                    END",
                    new
                    {
                        topic.Title,
                        topic.Summary,
                        topic.SourceType,
                        SourceUrl    = topic.SourceUrl ?? "",
                        topic.ThumbnailUrl,
                        topic.Publisher,
                        topic.PublishedAt,
                        Category = normalizedCategory,
                        CorrelationId = topic.CorrelationId ?? correlationId
                    }, tx);
            }
            tx.Commit();
            return new(batch.Count, inserted, batch.Count - inserted);
        }

        public async Task<IEnumerable<TrendingTopic>> GetUnprocessedAsync(int maxCount = 50)
        {
            using var conn = _context.CreateConnection();

            await conn.ExecuteAsync(@"
                UPDATE TrendingTopics
                SET IsProcessed = 1, ProcessedAt = GETUTCDATE()
                WHERE IsProcessed = 0 AND FetchedAt < DATEADD(HOUR, -72, GETUTCDATE());");

            return await conn.QueryAsync<TrendingTopic>(@"
                WITH RankedTopics AS (
                    SELECT *, ROW_NUMBER() OVER (
                        PARTITION BY SourceType ORDER BY FetchedAt DESC
                    ) AS SourceRank
                    FROM TrendingTopics
                    WHERE IsProcessed = 0
                      AND FetchedAt >= DATEADD(HOUR, -72, GETUTCDATE())
                )
                SELECT TOP (@MaxCount) *
                FROM RankedTopics
                ORDER BY SourceRank, FetchedAt DESC",
                new { MaxCount = maxCount });
        }

        public async Task MarkProcessedAsync(long id)
        {
            using var conn = _context.CreateConnection();

            await conn.ExecuteAsync(@"
                UPDATE TrendingTopics
                SET IsProcessed = 1, ProcessedAt = GETUTCDATE()
                WHERE Id = @Id",
                new { Id = id });
        }

        public async Task<IReadOnlyList<TrendingTopic>> ClaimEligibleAsync(int maxCount, TimeSpan leaseDuration)
        {
            using var conn = _context.CreateConnection();
            var leaseId = Guid.NewGuid();
            var rows = await conn.QueryAsync<TrendingTopic>(@"
                DECLARE @Now DATETIME2=GETUTCDATE();
                ;WITH candidates AS (
                    SELECT TOP (@MaxCount) * FROM TrendingTopics WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE (ProcessingStatus='Queued' OR (ProcessingStatus='RetryPending' AND NextAttemptAt<=@Now)
                           OR (ProcessingStatus='Processing' AND LeaseExpiresAt<@Now))
                      AND FetchedAt>=DATEADD(HOUR,-72,@Now)
                    ORDER BY FetchedAt
                )
                UPDATE candidates SET ProcessingStatus='Processing', LeaseId=@LeaseId,
                    LeaseExpiresAt=DATEADD(SECOND,@LeaseSeconds,@Now), LastAttemptAt=@Now, AttemptCount=AttemptCount+1
                OUTPUT inserted.*;", new { MaxCount=Math.Clamp(maxCount,1,100), LeaseId=leaseId, LeaseSeconds=(int)leaseDuration.TotalSeconds });
            return rows.ToList();
        }

        public async Task MarkRetryAsync(long id, string failureCode, int maxAttempts, TimeSpan baseDelay)
        {
            using var conn=_context.CreateConnection();
            await conn.ExecuteAsync(@"UPDATE TrendingTopics SET ProcessingStatus=CASE WHEN AttemptCount>=@MaxAttempts THEN 'Rejected' ELSE 'RetryPending' END,
                LastFailureCode=@FailureCode, NextAttemptAt=CASE WHEN AttemptCount>=@MaxAttempts THEN NULL ELSE DATEADD(SECOND,@BaseSeconds*POWER(CAST(2 AS FLOAT),CASE WHEN AttemptCount>8 THEN 8 ELSE AttemptCount-1 END),GETUTCDATE()) END,
                LeaseId=NULL,LeaseExpiresAt=NULL,IsProcessed=CASE WHEN AttemptCount>=@MaxAttempts THEN 1 ELSE 0 END,ProcessedAt=CASE WHEN AttemptCount>=@MaxAttempts THEN GETUTCDATE() ELSE NULL END WHERE Id=@Id",
                new { Id=id, FailureCode=SafeCode(failureCode), MaxAttempts=Math.Clamp(maxAttempts,1,20), BaseSeconds=Math.Clamp((int)baseDelay.TotalSeconds,1,86400) });
        }
        public async Task MarkRejectedAsync(long id,string failureCode) { using var conn=_context.CreateConnection(); await conn.ExecuteAsync("UPDATE TrendingTopics SET ProcessingStatus='Rejected',LastFailureCode=@Code,IsProcessed=1,ProcessedAt=GETUTCDATE(),LeaseId=NULL,LeaseExpiresAt=NULL WHERE Id=@Id",new{Id=id,Code=SafeCode(failureCode)}); }
        public async Task MarkCompletedAsync(long id,long pollId,string status) { var safe=status==TopicProcessingStatus.Published?TopicProcessingStatus.Published:TopicProcessingStatus.Review; using var conn=_context.CreateConnection(); await conn.ExecuteAsync("UPDATE TrendingTopics SET ProcessingStatus=@Status,GeneratedPollId=@PollId,IsProcessed=1,ProcessedAt=GETUTCDATE(),LeaseId=NULL,LeaseExpiresAt=NULL WHERE Id=@Id",new{Id=id,PollId=pollId,Status=safe}); }
        public async Task<PipelineBacklog> GetBacklogAsync() { using var conn=_context.CreateConnection(); return await conn.QuerySingleAsync<PipelineBacklog>(@"SELECT SUM(CASE WHEN ProcessingStatus='Queued' THEN 1 ELSE 0 END) Queued,SUM(CASE WHEN ProcessingStatus='Processing' THEN 1 ELSE 0 END) Processing,SUM(CASE WHEN ProcessingStatus='RetryPending' THEN 1 ELSE 0 END) RetryPending,MIN(CASE WHEN ProcessingStatus IN ('Queued','RetryPending') THEN COALESCE(NextAttemptAt,FetchedAt) END) OldestEligibleAt FROM TrendingTopics"); }
        public async Task<int> RequeueAsync(int maxCount) { using var conn=_context.CreateConnection(); return await conn.ExecuteScalarAsync<int>(@"DECLARE @Changed TABLE(Id bigint); WITH candidates AS (SELECT TOP (@Count) * FROM TrendingTopics WITH (UPDLOCK,READPAST) WHERE ProcessingStatus IN ('RetryPending','Rejected') ORDER BY LastAttemptAt) UPDATE candidates SET ProcessingStatus='RetryPending',NextAttemptAt=GETUTCDATE(),IsProcessed=0,ProcessedAt=NULL OUTPUT inserted.Id INTO @Changed; SELECT COUNT(*) FROM @Changed;",new{Count=Math.Clamp(maxCount,1,100)}); }
        public async Task<PipelineControlState> GetControlStateAsync() { using var conn=_context.CreateConnection(); return await conn.QuerySingleAsync<PipelineControlState>("SELECT GenerationPaused,UpdatedAt,UpdatedBy FROM PipelineControl WHERE Id=1"); }
        public async Task SetGenerationPausedAsync(bool paused,string? operatorId) { using var conn=_context.CreateConnection(); await conn.ExecuteAsync("UPDATE PipelineControl SET GenerationPaused=@Paused,UpdatedAt=GETUTCDATE(),UpdatedBy=@OperatorId WHERE Id=1",new{Paused=paused,OperatorId=operatorId}); }
        private static string SafeCode(string value) => new string(value.Where(c=>char.IsLetterOrDigit(c)||c=='_').Take(64).ToArray());
    }
}
