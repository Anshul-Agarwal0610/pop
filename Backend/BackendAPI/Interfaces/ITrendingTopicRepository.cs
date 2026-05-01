using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface ITrendingTopicRepository
    {
        /// <summary>Insert a batch of new trending topics (deduplicates by SourceUrl).</summary>
        Task SaveBatchAsync(IEnumerable<TrendingTopic> topics);

        /// <summary>Return all topics that have not yet been turned into polls.</summary>
        Task<IEnumerable<TrendingTopic>> GetUnprocessedAsync(int maxCount = 50);

        /// <summary>Mark a topic as processed (poll was generated from it).</summary>
        Task MarkProcessedAsync(long id);
    }
}
