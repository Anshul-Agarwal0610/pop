using BackendAPI.Services;
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
        /// Persist source items and group canonical URLs or matching recent titles.
        /// </summary>
        public async Task SaveBatchAsync(IEnumerable<TrendingTopic> topics)
            => _ = await SaveBatchWithResultAsync(topics);

        public async Task<TopicSaveResult> SaveBatchWithResultAsync(IEnumerable<TrendingTopic> topics, string? correlationId = null)
        {
            var batch = topics.Where(topic => TopicLanguagePolicy.IsSupported(topic.Language)).ToList();
            if (batch.Count == 0) return new(0, 0, 0);

            using var conn = _context.CreateConnection();
            var inserted = 0;

            conn.Open();
            using var tx = conn.BeginTransaction();
            try {
                await conn.ExecuteAsync("DECLARE @r int; EXEC @r=sp_getapplock @Resource='topic-ingestion',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51000,'Ingestion lock unavailable',1;",transaction:tx);
                var recent=(await conn.QueryAsync<TrendingTopic>("SELECT TOP(1000) * FROM TrendingTopics WHERE FetchedAt>=DATEADD(HOUR,-48,SYSUTCDATETIME()) ORDER BY FetchedAt DESC",transaction:tx)).ToList();
                foreach (var topic in batch)
                {
                    topic.Title = TopicEnrichment.CleanText(topic.Title);
                    topic.Summary = TopicEnrichment.CleanText(topic.Summary);
                    if (string.IsNullOrWhiteSpace(topic.Title)) continue;
                    var url=SourceIdentity.CanonicalUrl(topic.SourceUrl);
                    var key=SourceIdentity.Key(topic.SourceType,url,topic.Title);
                    var existing=await conn.QuerySingleOrDefaultAsync<long?>("SELECT TopicId FROM TopicSourceItems WHERE SourceKey=@Key",new {Key=key},tx);
                    existing ??= recent.FirstOrDefault(t => (!string.IsNullOrEmpty(url) && SourceIdentity.CanonicalUrl(t.SourceUrl)==url) ||
                        (topic.SourceType is not ("events" or "x" or "instagram") && t.SourceType is not ("events" or "x" or "instagram") && SameTopicTitle(t.Title,topic.Title)))?.Id;
                    if(existing is null) {
                        topic.Id=await conn.ExecuteScalarAsync<long>("""
                            INSERT TrendingTopics(Title,Summary,SourceType,SourceUrl,ThumbnailUrl,Publisher,PublishedAt,Category,FetchedAt,IsProcessed,ConversionStatus,AttemptCount,CorrelationId,SourceItemId,SourceTopic,Language,Region,EventStartUtc)
                            OUTPUT inserted.Id VALUES(@Title,@Summary,@SourceType,@SourceUrl,@ThumbnailUrl,@Publisher,@PublishedAt,@Category,SYSUTCDATETIME(),0,'Pending',0,@CorrelationId,@SourceItemId,@SourceTopic,@Language,@Region,@EventStartUtc)
                            """,new {topic.Title,topic.Summary,topic.SourceType,SourceUrl=url,topic.ThumbnailUrl,topic.Publisher,topic.PublishedAt,Category=CategoryCatalog.NormalizeName(topic.Category),CorrelationId=topic.CorrelationId??correlationId,topic.SourceItemId,topic.SourceTopic,topic.Language,topic.Region,topic.EventStartUtc},tx);
                        existing=topic.Id; recent.Add(topic); inserted++;
                    }
                    await conn.ExecuteAsync("""
                        IF EXISTS(SELECT 1 FROM TopicSourceItems WHERE SourceKey=@Key)
                            UPDATE TopicSourceItems SET LastSeenAtUtc=SYSUTCDATETIME() WHERE SourceKey=@Key;
                        ELSE INSERT TopicSourceItems(SourceKey,TopicId,SourceType,SourceUrl,Title,Summary,Publisher,PublishedAt,FirstSeenAtUtc,LastSeenAtUtc,SourceItemId,SourceTopic,Language,Region,EventStartUtc)
                            VALUES(@Key,@TopicId,@SourceType,@Url,@Title,@Summary,@Publisher,@PublishedAt,SYSUTCDATETIME(),SYSUTCDATETIME(),@SourceItemId,@SourceTopic,@Language,@Region,@EventStartUtc);
                        """,new {Key=key,TopicId=existing,topic.SourceType,Url=url,topic.Title,topic.Summary,topic.Publisher,topic.PublishedAt,topic.SourceItemId,topic.SourceTopic,topic.Language,topic.Region,topic.EventStartUtc},tx);
                }
                tx.Commit();
            } catch { tx.Rollback(); throw; }
            return new(batch.Count, inserted, batch.Count - inserted);
        }

        internal static bool SameTopicTitle(string a,string b)
        {
            // Conservative, non-AI grouping; preserve negation and numbers.
            static string Normalize(string value) => string.Join(' ',new string(value.ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)?c:' ').ToArray()).Split(' ',StringSplitOptions.RemoveEmptyEntries));
            return Normalize(a)==Normalize(b);
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

        public async Task<IEnumerable<TrendingTopic>> GetEligibleAsync(int maxCount, int retryLimit)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<TrendingTopic>(@"
                SELECT TOP (@MaxCount) * FROM TrendingTopics
                WHERE ConversionStatus IN ('Pending','RetryPending')
                  AND AttemptCount < @RetryLimit
                  AND (NextAttemptAt IS NULL OR NextAttemptAt <= GETUTCDATE())
                ORDER BY FetchedAt DESC", new { MaxCount = maxCount, RetryLimit = retryLimit });
        }

        public Task MarkConvertedAsync(long id, long pollId, string generationMethod) => UpdateAsync(@"
            UPDATE TrendingTopics SET ConversionStatus='Converted', IsProcessed=1, ProcessedAt=GETUTCDATE(),
              AttemptCount=AttemptCount+1, LastAttemptAt=GETUTCDATE(), LastFailureKind=NULL, LastFailureReason=NULL,
              LastGenerationMethod=@Method, GeneratedPollId=@PollId WHERE Id=@Id AND ConversionStatus IN ('Pending','RetryPending')",
            new { Id = id, PollId = pollId, Method = generationMethod });

        public Task RecordTransientFailureAsync(long id, int retryLimit, TimeSpan retryDelay, string reason, string attemptedMethod) => UpdateAsync(@"
            UPDATE TrendingTopics SET AttemptCount=AttemptCount+1, LastAttemptAt=GETUTCDATE(),
              ConversionStatus=CASE WHEN AttemptCount+1 >= @RetryLimit THEN 'NeedsReview' ELSE 'RetryPending' END,
              NextAttemptAt=CASE WHEN AttemptCount+1 >= @RetryLimit THEN NULL ELSE DATEADD(SECOND,@DelaySeconds,GETUTCDATE()) END,
              IsProcessed=CASE WHEN AttemptCount+1 >= @RetryLimit THEN 1 ELSE 0 END,
              ProcessedAt=CASE WHEN AttemptCount+1 >= @RetryLimit THEN GETUTCDATE() ELSE NULL END,
              LastFailureKind='ProviderTransientFailure', LastFailureReason=@Reason, LastGenerationMethod=@Method WHERE Id=@Id",
            new { Id = id, RetryLimit = retryLimit, DelaySeconds = (int)retryDelay.TotalSeconds, Reason = Truncate(reason), Method = attemptedMethod });

        public Task MarkNeedsReviewAsync(long id, string reason, string attemptedMethod) => TerminalAsync(id, "NeedsReview", reason, attemptedMethod);
        public Task MarkUnconvertibleAsync(long id, string reason, string attemptedMethod) => TerminalAsync(id, "Unconvertible", reason, attemptedMethod);
        private Task TerminalAsync(long id, string status, string reason, string method) => UpdateAsync(@"
            UPDATE TrendingTopics SET ConversionStatus=@Status, IsProcessed=1, ProcessedAt=GETUTCDATE(), AttemptCount=AttemptCount+1,
              LastAttemptAt=GETUTCDATE(), LastFailureKind=@Status, LastFailureReason=@Reason, LastGenerationMethod=@Method WHERE Id=@Id",
            new { Id = id, Status = status, Reason = Truncate(reason), Method = method });
        private async Task UpdateAsync(string sql, object args) { using var conn = _context.CreateConnection(); await conn.ExecuteAsync(sql, args); }
        private static string Truncate(string value) => value.Length <= 1000 ? value : value[..1000];

        public async Task MarkProcessedAsync(long id)
        {
            using var conn = _context.CreateConnection();

            await conn.ExecuteAsync(@"
                UPDATE TrendingTopics
                SET IsProcessed = 1, ProcessedAt = GETUTCDATE()
                WHERE Id = @Id",
                new { Id = id });
        }

        public async Task<IEnumerable<TrendingTopic>> ClaimDueAsync(int maxCount, TimeSpan leaseDuration)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<TrendingTopic>(@"
                ;WITH due AS (SELECT TOP (@MaxCount) * FROM TrendingTopics WITH (UPDLOCK,READPAST,ROWLOCK)
                  WHERE IsProcessed=0 AND (GenerationStatus IN ('Pending','RetryScheduled') OR
                    (GenerationStatus='InProgress' AND LeaseExpiresAtUtc<=GETUTCDATE()))
                  AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc<=GETUTCDATE()) ORDER BY FetchedAt DESC)
                UPDATE due SET GenerationStatus='InProgress',LeaseId=@LeaseId,
                  LeaseExpiresAtUtc=DATEADD(SECOND,@LeaseSeconds,GETUTCDATE()),AttemptCount=AttemptCount+1
                OUTPUT inserted.*;", new { MaxCount=maxCount, LeaseId=Guid.NewGuid(), LeaseSeconds=(int)leaseDuration.TotalSeconds });
        }

        public async Task ScheduleRetryAsync(long id, Guid leaseId, DateTimeOffset nextAttempt, LlmFailureClass failureClass, string? provider, string? detail)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(@"UPDATE TrendingTopics SET GenerationStatus='RetryScheduled',NextAttemptAtUtc=@Next,
              LastFailureClass=@Failure,LastFailureProvider=@Provider,LastFailureAtUtc=GETUTCDATE(),LastFailureDetail=LEFT(@Detail,500),
              LeaseId=NULL,LeaseExpiresAtUtc=NULL WHERE Id=@Id AND LeaseId=@LeaseId AND IsProcessed=0",
              new { Id=id,LeaseId=leaseId,Next=nextAttempt.UtcDateTime,Failure=failureClass.ToString(),Provider=provider,Detail=detail });
        }

        public async Task MarkTerminalAsync(long id, Guid leaseId, string decision)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(@"UPDATE TrendingTopics SET GenerationStatus='Terminal',TerminalDecision=LEFT(@Decision,500),
              IsProcessed=1,ProcessedAt=GETUTCDATE(),LeaseId=NULL,LeaseExpiresAtUtc=NULL WHERE Id=@Id AND LeaseId=@LeaseId",
              new { Id=id,LeaseId=leaseId,Decision=decision });
        }

        public async Task<PipelineBacklog> GetBacklogAsync()
        {
            using var conn = _context.CreateConnection();
            return await conn.QuerySingleAsync<PipelineBacklog>(@"
                SELECT
                  SUM(CASE WHEN IsProcessed=0 AND ConversionStatus='Pending' THEN 1 ELSE 0 END) Queued,
                  SUM(CASE WHEN IsProcessed=0 AND GenerationStatus='InProgress' THEN 1 ELSE 0 END) Processing,
                  SUM(CASE WHEN IsProcessed=0 AND ConversionStatus='RetryPending' THEN 1 ELSE 0 END) RetryPending,
                  MIN(CASE WHEN IsProcessed=0 THEN COALESCE(NextAttemptAt,FetchedAt) END) OldestEligibleAt
                FROM TrendingTopics");
        }

        public async Task<int> RequeueAsync(int maxCount)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteAsync(@"
                WITH candidates AS (
                  SELECT TOP (@Count) * FROM TrendingTopics WITH (UPDLOCK,READPAST)
                  WHERE ConversionStatus IN ('RetryPending','NeedsReview') ORDER BY LastAttemptAt)
                UPDATE candidates SET ConversionStatus='RetryPending',GenerationStatus='RetryScheduled',NextAttemptAt=GETUTCDATE(),NextAttemptAtUtc=GETUTCDATE(),AttemptCount=0,LeaseId=NULL,LeaseExpiresAtUtc=NULL,IsProcessed=0,ProcessedAt=NULL",
                new { Count = Math.Clamp(maxCount, 1, 100) });
        }

        public async Task<PipelineControlState> GetControlStateAsync()
        {
            using var conn = _context.CreateConnection();
            return await conn.QuerySingleAsync<PipelineControlState>("SELECT GenerationPaused,UpdatedAt,UpdatedBy FROM PipelineControl WHERE Id=1");
        }

        public async Task SetGenerationPausedAsync(bool paused, string? operatorId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync("UPDATE PipelineControl SET GenerationPaused=@Paused,UpdatedAt=GETUTCDATE(),UpdatedBy=@OperatorId WHERE Id=1",
                new { Paused = paused, OperatorId = operatorId });
        }
    }
}
