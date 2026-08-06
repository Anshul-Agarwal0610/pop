using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface ITrendingTopicRepository
    {
        /// <summary>Insert a batch of new trending topics (deduplicates by SourceUrl).</summary>
        Task<TopicSaveResult> SaveBatchAsync(IEnumerable<TrendingTopic> topics, string? correlationId = null);

        /// <summary>Return all topics that have not yet been turned into polls.</summary>
        Task<IEnumerable<TrendingTopic>> GetUnprocessedAsync(int maxCount = 50);

        /// <summary>Mark a topic as processed (poll was generated from it).</summary>
        Task MarkProcessedAsync(long id);
        Task<IReadOnlyList<TrendingTopic>> ClaimEligibleAsync(int maxCount, TimeSpan leaseDuration);
        Task MarkRetryAsync(long id, string failureCode, int maxAttempts, TimeSpan baseDelay);
        Task MarkRejectedAsync(long id, string failureCode);
        Task MarkCompletedAsync(long id, long pollId, string status);
        Task<PipelineBacklog> GetBacklogAsync();
        Task<int> RequeueAsync(int maxCount);
        Task<PipelineControlState> GetControlStateAsync();
        Task SetGenerationPausedAsync(bool paused, string? operatorId);
    }
}
