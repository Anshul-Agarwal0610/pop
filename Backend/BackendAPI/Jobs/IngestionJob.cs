using BackendAPI.Interfaces;
using Hangfire;
using BackendAPI.Models;
using BackendAPI.Observability;

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
        private readonly PipelineMetrics _metrics;
        private readonly IPipelineRuntimeHealth _health;

        public IngestionJob(
            IRssIngestionService rss,
            IYouTubeIngestionService youtube,
            IGNewsIngestionService gnews,
            ITrendingTopicRepository repo,
            ILogger<IngestionJob> logger, PipelineMetrics metrics, IPipelineRuntimeHealth health)
        {
            _rss     = rss;
            _youtube = youtube;
            _gnews   = gnews;
            _repo    = repo;
            _logger  = logger;
            _metrics = metrics;
            _health = health;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task RunAsync(string? source = null, int maxTopics = 100)
        {
            using var activity=PipelineActivities.Start("pipeline.ingestion");
            var correlationId=PipelineActivities.CorrelationId(activity);
            using var scope=_logger.BeginScope(new Dictionary<string,object>{{"CorrelationId",correlationId}});
            _logger.LogInformation("Ingestion run started at {Time} for {Source}", DateTime.UtcNow, source ?? "all");

            // Fetch all sources in parallel
            var tasks = new List<Task<IngestionFetchResult>>();
            if (source is null or "rss") tasks.Add(_rss.FetchWithResultAsync());
            if (source is null or "youtube") tasks.Add(_youtube.FetchWithResultAsync());
            if (source is null or "gnews") tasks.Add(_gnews.FetchWithResultAsync());
            if (tasks.Count==0) throw new ArgumentOutOfRangeException(nameof(source));

            var results = await Task.WhenAll(tasks);

            foreach(var result in results)
            {
                _metrics.Ingestion("fetched",result.Topics.Count,result.Source);
                _metrics.IngestionDuration(result.Source,result.Duration);
                var state=result.State switch { IngestionSourceState.Disabled=>ProviderOperationalState.Disabled,IngestionSourceState.Misconfigured=>ProviderOperationalState.Misconfigured,IngestionSourceState.CoolingDown=>ProviderOperationalState.CoolingDown,_=>ProviderOperationalState.Enabled };
                _health.RecordIngestion(result.Source,state,result.Topics.Count,result.SuccessCount>0,result.RateLimitCount>0,result.ErrorCode);
            }
            var allTopics = results.SelectMany(r => r.Topics).Take(Math.Clamp(maxTopics,1,500)).ToList();
            allTopics.ForEach(t=>t.CorrelationId=correlationId);
            _logger.LogInformation("[IngestionJob] Total topics fetched: {Count}", allTopics.Count);

            if (allTopics.Count == 0)
            {
                _logger.LogWarning("[IngestionJob] No topics fetched — nothing to save");
                return;
            }

            var saved=await _repo.SaveBatchWithResultAsync(allTopics,correlationId);
            _metrics.Ingestion("queued",saved.Inserted,"all");
            _metrics.Ingestion("deduplicated",saved.Deduplicated,"all");
            _logger.LogInformation("Ingestion saved {Queued} topics and deduplicated {Deduplicated}",saved.Inserted,saved.Deduplicated);
        }
    }
}
