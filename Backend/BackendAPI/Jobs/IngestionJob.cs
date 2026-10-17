using BackendAPI.Data;
using Dapper;
using BackendAPI.Interfaces;
using Hangfire;
using BackendAPI.Models;
using BackendAPI.Observability;

namespace BackendAPI.Jobs
{
    /// <summary>
    /// US-06 — Hangfire recurring job that orchestrates all ingestion sources.
    /// Checks a persisted five-hour intake schedule every five minutes.
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
        private readonly DapperContext _context;
        private readonly IConfiguration _config;
        private readonly IEnumerable<BackendAPI.Services.IAdditionalIngestionSource> _additional;

        public IngestionJob(
            IRssIngestionService rss,
            IYouTubeIngestionService youtube,
            IGNewsIngestionService gnews,
            ITrendingTopicRepository repo,
            ILogger<IngestionJob> logger, PipelineMetrics metrics, IPipelineRuntimeHealth health, DapperContext context, IConfiguration config,
            IEnumerable<BackendAPI.Services.IAdditionalIngestionSource> additional)
        {
            _rss     = rss;
            _youtube = youtube;
            _gnews   = gnews;
            _repo    = repo;
            _logger  = logger;
            _metrics = metrics;
            _health = health; _context = context; _config = config;
            _additional = additional;
        }

        // Poll every five minutes, but persist the next due instant so the actual
        // intake interval remains five hours across midnight and process restarts.
        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task RunScheduledAsync()
        {
            using var db = _context.CreateConnection();
            var due = await db.ExecuteAsync("UPDATE PipelineSchedule SET NextRunAtUtc=DATEADD(HOUR,5,SYSUTCDATETIME()) WHERE Name='ingestion' AND NextRunAtUtc<=SYSUTCDATETIME()");
            if(due == 0) return;
            try { await RunAsync(null, Math.Clamp(_config.GetValue("Pipeline:MaxIngestionBatch",100),1,500)); }
            catch {
                await db.ExecuteAsync("UPDATE PipelineSchedule SET NextRunAtUtc=DATEADD(MINUTE,15,SYSUTCDATETIME()) WHERE Name='ingestion'");
                throw;
            }
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
            source = source?.ToLowerInvariant();
            if (source == "rss" || (source is null && !_config.GetValue("PhaseOne:ApiSourcesOnly", false))) tasks.Add(_rss.FetchWithResultAsync());
            if (source is null or "youtube") tasks.Add(_config.GetValue("Ingestion:youtube:Disabled",false) ? Task.FromResult(IngestionFetchResult.Disabled("youtube")) : _youtube.FetchWithResultAsync());
            if (source is null or "gnews") tasks.Add(_config.GetValue("Ingestion:gnews:Disabled",false) ? Task.FromResult(IngestionFetchResult.Disabled("gnews")) : _gnews.FetchWithResultAsync());
            foreach(var provider in _additional.Where(p=>source is null || p.Name==source)) tasks.Add(provider.FetchWithResultAsync());
            if (tasks.Count==0) throw new ArgumentOutOfRangeException(nameof(source));

            var results = await Task.WhenAll(tasks);

            foreach(var result in results)
            {
                _metrics.Ingestion("fetched",result.Topics.Count,result.Source);
                _metrics.IngestionDuration(result.Source,result.Duration);
                var state=result.State switch { IngestionSourceState.Disabled=>ProviderOperationalState.Disabled,IngestionSourceState.Misconfigured=>ProviderOperationalState.Misconfigured,IngestionSourceState.CoolingDown=>ProviderOperationalState.CoolingDown,IngestionSourceState.Failed=>ProviderOperationalState.Failed,_=>ProviderOperationalState.Enabled };
                _health.RecordIngestion(result.Source,state,result.Topics.Count,result.SuccessCount>0,result.RateLimitCount>0,result.ErrorCode);
            }
            var allTopics = results.SelectMany(r => r.Topics.Select((topic, rank) => new { topic, rank })).OrderBy(x => x.rank).Select(x => x.topic).Take(Math.Clamp(maxTopics,1,500)).ToList();
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
