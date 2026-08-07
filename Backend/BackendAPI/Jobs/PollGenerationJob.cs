using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using BackendAPI.Services.Llm;
using BackendAPI.Observability;
using Hangfire;

namespace BackendAPI.Jobs;

public class PollGenerationJob
{
    private readonly ITrendingTopicRepository _topics;
    private readonly IPollsRepository _polls;
    private readonly IPollGenerationService _generator;
    private readonly IConfiguration _config;
    private readonly ILogger<PollGenerationJob> _logger;
    private readonly IRetryDelayPolicy _retryPolicy;
    private readonly TimeProvider _time;
    private readonly IGeneratedPollQualityGate _qualityGate;
    private readonly PipelineMetrics _metrics;

    public PollGenerationJob(ITrendingTopicRepository topics, IPollsRepository polls, IPollGenerationService generator,
        IConfiguration config, ILogger<PollGenerationJob> logger, IRetryDelayPolicy retryPolicy, TimeProvider time,
        IGeneratedPollQualityGate qualityGate, PipelineMetrics metrics)
        => (_topics, _polls, _generator, _config, _logger, _retryPolicy, _time, _qualityGate, _metrics) =
            (topics, polls, generator, config, logger, retryPolicy, time, qualityGate, metrics);

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(int? requestedCount = null)
    {
        if ((await _topics.GetControlStateAsync()).GenerationPaused)
        { _logger.LogInformation("Poll generation is paused"); return; }
        var limit = Math.Max(1, _config.GetValue("PollGen:RetryLimit", 3));
        var delay = TimeSpan.FromSeconds(Math.Max(1, _config.GetValue("PollGen:RetryBaseDelaySeconds", 900)));
        var configuredCount = Math.Clamp(_config.GetValue("Pipeline:MaxGenerationBatch", 5), 1, 100);
        var count = Math.Min(requestedCount ?? configuredCount, configuredCount);
        foreach (var topic in await _topics.GetEligibleAsync(count, limit))
        {
            using var activity = PipelineActivities.Start("pipeline.generate.topic", topic.CorrelationId);
            var correlationId = topic.CorrelationId ?? PipelineActivities.CorrelationId(activity);
            using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["TopicId"] = topic.Id });
            var outcome = await _generator.GenerateAsync(topic);
            var method = outcome.AttemptedMethod ?? GenerationMethods.ManualReview;
            if (outcome.Outcome == GenerationOutcome.Succeeded && outcome.Poll is not null)
            {
                _metrics.Generation("converted");
                if (!BinaryPublicationValidator.Validate(outcome.Poll, topic, out var gateReason))
                {
                    await _topics.MarkUnconvertibleAsync(topic.Id, gateReason, method);
                    continue;
                }
                try
                {
                    var decision = await _qualityGate.EvaluateAsync(topic, outcome.Poll, outcome.Poll.Options);
                    _logger.LogInformation("[PollQuality] TopicId={TopicId} Disposition={Disposition} Reasons={ReasonCodes} Rules={RulesVersion} ScoreBucket={ScoreBucket}",
                        topic.Id, decision.Disposition, string.Join(',', decision.ReasonCodes), decision.RulesVersion,
                        Math.Floor(decision.OverallScore * 10) / 10);
                    if (decision.Disposition == PollQualityDisposition.Rejected)
                    {
                        _metrics.Generation("rejected");
                        await _polls.RecordRejectedQualityDecisionAsync(topic.Id, decision);
                        await _topics.MarkUnconvertibleAsync(topic.Id, string.Join(',', decision.ReasonCodes), method);
                        continue;
                    }
                    var pollId = await _polls.CreateAsync(new CreatePollRequest
                    {
                        Question = outcome.Poll.Proposition, Description = topic.Summary, Category = outcome.Poll.Category,
                        ExpiresAt = DateTime.UtcNow.AddHours(48), Options = outcome.Poll.Options, SourceType = topic.SourceType,
                        SourceUrl = topic.SourceUrl, ThumbnailUrl = topic.ThumbnailUrl,
                        IsAIGenerated = outcome.Poll.GenerationMethod == GenerationMethods.Llm,
                        GenerationMethod = outcome.Poll.GenerationMethod, TrendingTopicId = topic.Id,
                        GenerationProvider = outcome.Poll.GenerationProvider, GenerationModel = outcome.Poll.GenerationModel,
                        QualityDecision = decision, AutoPublish = decision.Disposition == PollQualityDisposition.Accepted,
                        ModerationReason = outcome.Poll.ReviewNotes
                    });
                    await _topics.MarkConvertedAsync(topic.Id, pollId, outcome.Poll.GenerationMethod);
                    _metrics.Generation(decision.Disposition == PollQualityDisposition.Accepted ? "published" : "review");
                }
                catch (Exception ex) { _logger.LogError(ex, "Failed to persist poll for topic {TopicId}; topic remains eligible", topic.Id); throw; }
            }
            else if (outcome.Outcome == GenerationOutcome.ProviderTransientFailure)
            {
                _metrics.Generation("retried");
                var now = _time.GetUtcNow();
                var next = _retryPolicy.GetNextAttempt(topic.AttemptCount + 1, now, outcome.RetryAtUtc);
                var retryDelay = next - now;
                await _topics.RecordTransientFailureAsync(topic.Id, limit, retryDelay > delay ? retryDelay : delay,
                    outcome.Reason ?? "Transient provider failure", method);
            }
            else if (outcome.Outcome == GenerationOutcome.ProviderPermanentFailure)
            {
                _metrics.Generation("review");
                await _topics.MarkNeedsReviewAsync(topic.Id, outcome.Reason ?? "Permanent provider failure", method);
            }
            else
            {
                _metrics.Generation("rejected");
                await _topics.MarkUnconvertibleAsync(topic.Id, outcome.Reason ?? "No defensible proposition", method);
            }
        }
    }
}
