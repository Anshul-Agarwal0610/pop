using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface ITrendingTopicRepository
    {
        /// <summary>Insert a batch of new trending topics (deduplicates by SourceUrl).</summary>
        Task SaveBatchAsync(IEnumerable<TrendingTopic> topics);

        /// <summary>Return all topics that have not yet been turned into polls.</summary>
        Task<IEnumerable<TrendingTopic>> GetUnprocessedAsync(int maxCount = 50);
        Task<IEnumerable<TrendingTopic>> ClaimDueAsync(int maxCount, TimeSpan leaseDuration);
        Task ScheduleRetryAsync(long id, Guid leaseId, DateTimeOffset nextAttempt, LlmFailureClass failureClass, string? provider, string? detail);
        Task MarkTerminalAsync(long id, Guid leaseId, string decision);

        /// <summary>Mark a topic as processed (poll was generated from it).</summary>
        Task MarkProcessedAsync(long id);
    }
}
