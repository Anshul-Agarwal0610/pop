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
                        WHERE SourceUrl = @SourceUrl AND SourceUrl <> ''
                    )
                    BEGIN
                        INSERT INTO TrendingTopics
                            (Title, Summary, SourceType, SourceUrl, ThumbnailUrl, Category, FetchedAt, IsProcessed)
                        VALUES
                            (@Title, @Summary, @SourceType, @SourceUrl, @ThumbnailUrl, @Category, GETUTCDATE(), 0)
                    END",
                    new
                    {
                        topic.Title,
                        topic.Summary,
                        topic.SourceType,
                        SourceUrl    = topic.SourceUrl ?? "",
                        topic.ThumbnailUrl,
                        Category = normalizedCategory
                    });
            }
        }

        public async Task<IEnumerable<TrendingTopic>> GetUnprocessedAsync(int maxCount = 50)
        {
            using var conn = _context.CreateConnection();

            return await conn.QueryAsync<TrendingTopic>(@"
                SELECT TOP (@MaxCount) *
                FROM TrendingTopics
                WHERE IsProcessed = 0
                ORDER BY FetchedAt DESC",
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
    }
}
