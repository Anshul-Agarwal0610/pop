using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    /// <summary>Contract for a single ingestion source (RSS, YouTube, GNews …).</summary>
    public interface IRssIngestionService
    {
        Task<IEnumerable<TrendingTopic>> FetchAsync();
        async Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default) { var started=DateTime.UtcNow; var topics=(await FetchAsync()).ToList(); return new("rss",IngestionSourceState.Enabled,topics,1,1,0,DateTime.UtcNow-started); }
    }

    public interface IYouTubeIngestionService
    {
        Task<IEnumerable<TrendingTopic>> FetchAsync();
        Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default);
    }

    public interface IGNewsIngestionService
    {
        Task<IEnumerable<TrendingTopic>> FetchAsync();
        Task<IngestionFetchResult> FetchWithResultAsync(CancellationToken token = default);
    }
}
