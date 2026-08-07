using BackendAPI.Interfaces;
using Hangfire;

namespace BackendAPI.Jobs
{
    /// <summary>
    /// US-06 — Hangfire recurring job that orchestrates all ingestion sources.
    /// Scheduled: every 30 minutes (cron: "*/30 * * * *").
    /// Runs RSS, YouTube, and GNews in parallel, merges results, deduplicates, saves to TrendingTopics.
    /// </summary>
    public class IngestionJob
    {
        private readonly IRssIngestionService     _rss;
        private readonly IYouTubeIngestionService _youtube;
        private readonly IGNewsIngestionService   _gnews;
        private readonly ITrendingTopicRepository _repo;
        private readonly ILogger<IngestionJob>    _logger;

        public IngestionJob(
            IRssIngestionService rss,
            IYouTubeIngestionService youtube,
            IGNewsIngestionService gnews,
            ITrendingTopicRepository repo,
            ILogger<IngestionJob> logger)
        {
            _rss     = rss;
            _youtube = youtube;
            _gnews   = gnews;
            _repo    = repo;
            _logger  = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task RunAsync()
        {
            _logger.LogInformation("[IngestionJob] Starting ingestion run at {Time}", DateTime.UtcNow);

            // Fetch all sources in parallel
            var tasks = new[]
            {
                _rss.FetchAsync(),
                _youtube.FetchAsync(),
                _gnews.FetchAsync()
            };

            var results = await Task.WhenAll(tasks);

            var allTopics = results.SelectMany(r => r).ToList();
            _logger.LogInformation("[IngestionJob] Total topics fetched: {Count}", allTopics.Count);

            if (allTopics.Count == 0)
            {
                _logger.LogWarning("[IngestionJob] No topics fetched — nothing to save");
                return;
            }

            await _repo.SaveBatchAsync(allTopics);
            _logger.LogInformation("[IngestionJob] Saved batch to TrendingTopics (dedup applied)");
        }
    }
}
