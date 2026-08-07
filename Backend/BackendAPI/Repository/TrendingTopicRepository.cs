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
        public async Task SaveBatchAsync(IEnumerable<TrendingTopic> topics)
            => _ = await SaveBatchWithResultAsync(topics);

        public async Task<TopicSaveResult> SaveBatchWithResultAsync(IEnumerable<TrendingTopic> topics, string? correlationId = null)
        {
            var batch = topics.ToList();
            if (batch.Count == 0) return new(0, 0, 0);

            using var conn = _context.CreateConnection();
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
                            (Title, Summary, SourceType, SourceUrl, ThumbnailUrl, Publisher, PublishedAt, Category, FetchedAt, IsProcessed, ConversionStatus, AttemptCount, CorrelationId)
                        VALUES
                            (@Title, @Summary, @SourceType, @SourceUrl, @ThumbnailUrl, @Publisher, @PublishedAt, @Category, GETUTCDATE(), 0, 'Pending', 0, @CorrelationId)
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
                    });
            }
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
                UPDATE candidates SET ConversionStatus='RetryPending',NextAttemptAt=GETUTCDATE(),IsProcessed=0,ProcessedAt=NULL",
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
