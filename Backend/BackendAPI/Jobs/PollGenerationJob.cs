using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services.Llm;
using Hangfire;
using Microsoft.Extensions.Options;

namespace BackendAPI.Jobs;

public sealed class PollGenerationJob(
    ITrendingTopicRepository topics,
    IPollsRepository polls,
    IPollGenerationService generator,
    IRetryDelayPolicy retryPolicy,
    IOptions<PollGenerationOptions> options,
    TimeProvider time,
    ILogger<PollGenerationJob> logger)
{
    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync()
    {
        var claimed = await topics.ClaimDueAsync(5, TimeSpan.FromSeconds(options.Value.TopicLeaseSeconds));
        foreach (var topic in claimed)
        {
            if (topic.LeaseId is not { } leaseId) continue;
            try
            {
                var outcome = await generator.GenerateAsync(topic);
                switch (outcome.Kind)
                {
                    case GenerationOutcomeKind.Poll:
                        var generated = outcome.Poll!;
                        await polls.CompleteGeneratedPollAsync(topic.Id, leaseId, new CreatePollRequest
                        {
                            Question=generated.Question, Description=topic.Summary, Category=generated.Category,
                            ExpiresAt=time.GetUtcNow().UtcDateTime.AddHours(48), Options=generated.Options,
                            SourceType=topic.SourceType,SourceUrl=topic.SourceUrl,ThumbnailUrl=topic.ThumbnailUrl,
                            SourceTopicId=topic.Id,IsAIGenerated=true,ModerationReason=generated.ReviewNotes
                        });
                        break;
                    case GenerationOutcomeKind.TerminalContentDecision:
                    case GenerationOutcomeKind.TerminalFailure:
                        await topics.MarkTerminalAsync(topic.Id, leaseId, outcome.Reason ?? outcome.FailureClass.ToString());
                        break;
                    default:
                        if (topic.AttemptCount >= options.Value.MaxAttemptsPerTopic)
                            await topics.MarkTerminalAsync(topic.Id, leaseId, $"Retry exhausted: {outcome.Reason}");
                        else
                            await topics.ScheduleRetryAsync(topic.Id, leaseId,
                                retryPolicy.GetNextAttempt(topic.AttemptCount, time.GetUtcNow(), outcome.RetryAtUtc),
                                outcome.FailureClass, outcome.Provider, outcome.Reason);
                        break;
                }
            }
            catch (Exception ex)
            {
                var next = retryPolicy.GetNextAttempt(topic.AttemptCount, time.GetUtcNow());
                await topics.ScheduleRetryAsync(topic.Id, leaseId, next, LlmFailureClass.TransientServer, null, "Persistence or orchestration failure");
                logger.LogError(ex, "Poll generation failed for topic {TopicId}; retry at {NextAttempt}", topic.Id, next);
            }
        }
    }
}
