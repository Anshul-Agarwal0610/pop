using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface ITrendingTopicRepository
{
    Task SaveBatchAsync(IEnumerable<TrendingTopic> topics);
    async Task<TopicSaveResult> SaveBatchWithResultAsync(IEnumerable<TrendingTopic> topics, string? correlationId = null)
    { var batch = topics.ToList(); await SaveBatchAsync(batch); return new(batch.Count, batch.Count, 0); }
    Task<IEnumerable<TrendingTopic>> GetEligibleAsync(int maxCount, int retryLimit);
    Task MarkConvertedAsync(long id, long pollId, string generationMethod);
    Task RecordTransientFailureAsync(long id, int retryLimit, TimeSpan retryDelay, string reason, string attemptedMethod);
    Task MarkNeedsReviewAsync(long id, string reason, string attemptedMethod);
    Task MarkUnconvertibleAsync(long id, string reason, string attemptedMethod);
    Task<IEnumerable<TrendingTopic>> GetUnprocessedAsync(int maxCount = 50);
    Task MarkProcessedAsync(long id);
    Task<PipelineBacklog> GetBacklogAsync();
    Task<int> RequeueAsync(int maxCount);
    Task<PipelineControlState> GetControlStateAsync();
    Task SetGenerationPausedAsync(bool paused, string? operatorId);
}
