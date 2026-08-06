using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Hangfire;
using BackendAPI.Observability;

namespace BackendAPI.Jobs
{
    /// <summary>
    /// Hangfire recurring job that reads unprocessed TrendingTopics,
    /// calls the poll-generation provider for each, persists the resulting poll,
    /// and marks the topic as processed.
    /// </summary>
    public class PollGenerationJob
    {
        private readonly ITrendingTopicRepository _topicRepo;
        private readonly IPollsRepository _pollsRepo;
        private readonly IPollGenerationService _generator;
        private readonly ILogger<PollGenerationJob> _logger;
        private readonly PipelineMetrics _metrics;
        private readonly IConfiguration _configuration;

        // Polls expire 48 hours after creation by default.
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(48);

        // Delay between LLM calls to stay under free-tier rate limits.
        private static readonly TimeSpan LlmDelay = TimeSpan.FromSeconds(13);

        public PollGenerationJob(
            ITrendingTopicRepository topicRepo,
            IPollsRepository pollsRepo,
            IPollGenerationService generator,
            ILogger<PollGenerationJob> logger, PipelineMetrics metrics, IConfiguration configuration)
        {
            _topicRepo = topicRepo;
            _pollsRepo = pollsRepo;
            _generator = generator;
            _logger = logger;
            _metrics = metrics;
            _configuration = configuration;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task RunAsync(int? requestedCount = null)
        {
            _logger.LogInformation("[PollGenerationJob] Starting at {Time}", DateTime.UtcNow);

            if ((await _topicRepo.GetControlStateAsync()).GenerationPaused) { _logger.LogInformation("Poll generation is paused"); return; }
            var maxConfigured=Math.Clamp(_configuration.GetValue("Pipeline:MaxGenerationBatch",5),1,100);
            var topics = (await _topicRepo.ClaimEligibleAsync(Math.Min(requestedCount ?? maxConfigured,maxConfigured),TimeSpan.FromMinutes(10))).ToList();

            if (topics.Count == 0)
            {
                _logger.LogInformation("[PollGenerationJob] No unprocessed topics. Nothing to do.");
                return;
            }

            _logger.LogInformation("[PollGenerationJob] Processing {Count} topics", topics.Count);

            int created = 0, skipped = 0;

            foreach (var topic in topics)
            {
                using var activity=PipelineActivities.Start("pipeline.generate.topic",topic.CorrelationId);
                var correlationId=topic.CorrelationId ?? PipelineActivities.CorrelationId(activity);
                using var scope=_logger.BeginScope(new Dictionary<string,object>{{"CorrelationId",correlationId},{"TopicId",topic.Id}});
                var outcome = await _generator.GenerateWithOutcomeAsync(topic);
                var generated = outcome.Result;

                if (generated == null)
                {
                    if(outcome.Kind==PollGenerationOutcomeKind.RetryableFailure) { await _topicRepo.MarkRetryAsync(topic.Id,outcome.FailureCode??"provider_failure",_configuration.GetValue("Pipeline:MaxAttempts",3),TimeSpan.FromSeconds(_configuration.GetValue("Pipeline:RetryBaseSeconds",60))); _metrics.Generation("retried"); }
                    else { await _topicRepo.MarkRejectedAsync(topic.Id,outcome.FailureCode??"quality_rejection"); _metrics.Generation("rejected"); }
                    _logger.LogWarning("Topic generation ended with {Outcome} and {FailureCode}",outcome.Kind,outcome.FailureCode);
                    skipped++;
                    continue;
                }

                _metrics.Generation("converted");

                await Task.Delay(LlmDelay);

                try
                {
                    var request = new CreatePollRequest
                    {
                        Question = generated.Proposition,
                        Description = topic.Summary,
                        Category = generated.Category,
                        ExpiresAt = DateTime.UtcNow.Add(DefaultExpiry),
                        Options = GeneratedPollContract.CanonicalOptions.ToList(),
                        SourceType = topic.SourceType,
                        SourceUrl = topic.SourceUrl,
                        ThumbnailUrl = topic.ThumbnailUrl,
                        IsAIGenerated = true,
                        AutoPublish = !TopicEnrichment.RequiresHumanReview(topic)
                            && generated.QualityWarnings.All(warning =>
                                warning.StartsWith("Generated with the deterministic fallback", StringComparison.OrdinalIgnoreCase)),
                        ModerationReason = generated.ReviewNotes
                    };

                    var pollId = await _pollsRepo.CreateAsync(request);
                    var status=request.AutoPublish?TopicProcessingStatus.Published:TopicProcessingStatus.Review;
                    await _topicRepo.MarkCompletedAsync(topic.Id,pollId,status);
                    _metrics.Generation(status==TopicProcessingStatus.Published?"published":"review");

                    _logger.LogInformation(
                        "Created generated poll {PollId} from topic {TopicId}; moderation={ModerationStatus}; similar={HasSimilarPoll}",
                        pollId,
                        topic.Id,
                        status,
                        generated.SimilarPollId is not null);

                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to persist generated poll for topic {TopicId}", topic.Id);
                }
            }

            _logger.LogInformation(
                "[PollGenerationJob] Done. Created: {Created}, skipped: {Skipped}",
                created,
                skipped);
        }
    }
}
