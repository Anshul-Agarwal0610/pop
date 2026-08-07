using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using BackendAPI.Services.Llm;
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

    public PollGenerationJob(ITrendingTopicRepository topics, IPollsRepository polls, IPollGenerationService generator,
        IConfiguration config, ILogger<PollGenerationJob> logger, IRetryDelayPolicy retryPolicy, TimeProvider time)
        => (_topics, _polls, _generator, _config, _logger, _retryPolicy, _time) = (topics, polls, generator, config, logger, retryPolicy, time);

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync()
    {
        var limit = Math.Max(1, _config.GetValue("PollGen:RetryLimit", 3));
        var delay = TimeSpan.FromSeconds(Math.Max(1, _config.GetValue("PollGen:RetryBaseDelaySeconds", 900)));
        foreach (var topic in await _topics.GetEligibleAsync(5, limit))
        {
            var outcome = await _generator.GenerateAsync(topic);
            var method = outcome.AttemptedMethod ?? GenerationMethods.ManualReview;
            if (outcome.Outcome == GenerationOutcome.Succeeded && outcome.Poll is not null)
            {
                if (!BinaryPublicationValidator.Validate(outcome.Poll, topic, out var gateReason))
                {
                    await _topics.MarkUnconvertibleAsync(topic.Id, gateReason, method);
                    continue;
                }
                try
                {
                    var pollId = await _polls.CreateAsync(new CreatePollRequest
                    {
                        Question = outcome.Poll.Proposition, Description = topic.Summary, Category = outcome.Poll.Category,
                        ExpiresAt = DateTime.UtcNow.AddHours(48), Options = outcome.Poll.Options, SourceType = topic.SourceType,
                        SourceUrl = topic.SourceUrl, ThumbnailUrl = topic.ThumbnailUrl,
                        IsAIGenerated = outcome.Poll.GenerationMethod == GenerationMethods.Llm,
                        GenerationMethod = outcome.Poll.GenerationMethod, TrendingTopicId = topic.Id,
                        GenerationProvider = outcome.Poll.GenerationProvider, GenerationModel = outcome.Poll.GenerationModel,
                        ModerationReason = outcome.Poll.ReviewNotes
                    });
                    await _topics.MarkConvertedAsync(topic.Id, pollId, outcome.Poll.GenerationMethod);
                }
                catch (Exception ex) { _logger.LogError(ex, "Failed to persist poll for topic {TopicId}; topic remains eligible", topic.Id); throw; }
            }
            else if (outcome.Outcome == GenerationOutcome.ProviderTransientFailure)
            {
                var now = _time.GetUtcNow();
                var next = _retryPolicy.GetNextAttempt(topic.AttemptCount + 1, now, outcome.RetryAtUtc);
                var retryDelay = next - now;
                await _topics.RecordTransientFailureAsync(topic.Id, limit, retryDelay > delay ? retryDelay : delay,
                    outcome.Reason ?? "Transient provider failure", method);
            }
            else if (outcome.Outcome == GenerationOutcome.ProviderPermanentFailure)
                await _topics.MarkNeedsReviewAsync(topic.Id, outcome.Reason ?? "Permanent provider failure", method);
            else
                await _topics.MarkUnconvertibleAsync(topic.Id, outcome.Reason ?? "No defensible proposition", method);
        }
    }
}
