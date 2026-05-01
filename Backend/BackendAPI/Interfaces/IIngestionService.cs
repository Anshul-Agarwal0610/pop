using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    /// <summary>Contract for a single ingestion source (RSS, YouTube, GNews …).</summary>
    public interface IRssIngestionService
    {
        Task<IEnumerable<TrendingTopic>> FetchAsync();
    }

    public interface IYouTubeIngestionService
    {
        Task<IEnumerable<TrendingTopic>> FetchAsync();
    }

    public interface IGNewsIngestionService
    {
        Task<IEnumerable<TrendingTopic>> FetchAsync();
    }
}
