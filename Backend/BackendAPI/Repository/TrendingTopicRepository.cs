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
                            (Title, Summary, SourceType, SourceUrl, ThumbnailUrl, Publisher, PublishedAt, Category, FetchedAt, IsProcessed)
                        VALUES
                            (@Title, @Summary, @SourceType, @SourceUrl, @ThumbnailUrl, @Publisher, @PublishedAt, @Category, GETUTCDATE(), 0)
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
