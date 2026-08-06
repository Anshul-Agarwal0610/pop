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
        {
            if (!topics.Any()) return;

            using var conn = _context.CreateConnection();

            foreach (var topic in topics)
            {
                var normalizedCategory = CategoryCatalog.NormalizeName(topic.Category);

                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (
                        SELECT 1 FROM TrendingTopics
                        WHERE (SourceUrl = @SourceUrl AND SourceUrl <> '')
                           OR (LOWER(LTRIM(RTRIM(Title))) = LOWER(LTRIM(RTRIM(@Title)))
                               AND FetchedAt >= DATEADD(HOUR, -48, GETUTCDATE()))
                    )
                    BEGIN
                        INSERT INTO TrendingTopics
                            (Title, Summary, SourceType, SourceUrl, ThumbnailUrl, Publisher, PublishedAt, Category, FetchedAt, IsProcessed, ConversionStatus, AttemptCount)
                        VALUES
                            (@Title, @Summary, @SourceType, @SourceUrl, @ThumbnailUrl, @Publisher, @PublishedAt, @Category, GETUTCDATE(), 0, 'Pending', 0)
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
                        Category = normalizedCategory
                    });
            }
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
    }
}
