using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Hangfire;

namespace BackendAPI.Jobs;

public sealed class GeneratedPollRegenerationJob(
    IGeneratedPollCleanupRepository cleanupRepository,
    IPollGenerationService generator,
    IGeneratedPollQualityGate qualityGate,
    IPollsRepository pollsRepository,
    ILogger<GeneratedPollRegenerationJob> logger)
{
    [DisableConcurrentExecution(600)]
    public async Task RunAsync(int maxRecords)
    {
        if (maxRecords is <= 0 or > GeneratedPollCleanupPolicy.MaximumBatchSize) throw new ArgumentOutOfRangeException(nameof(maxRecords));
        var items = await cleanupRepository.ClaimRegenerationBatchAsync(maxRecords);
        foreach (var item in items)
        {
            try
            {
                if (item.ReplacementPollId is { } existingReplacementId)
                {
                    await cleanupRepository.CompleteRegenerationAsync(item, existingReplacementId);
                    continue;
                }
                var topic = await cleanupRepository.ResolveTopicAsync(item);
                if (topic is null) throw new InvalidOperationException("No unique trending topic provenance; manual review is required.");
                var outcome = await generator.GenerateAsync(topic);
                if (outcome.Outcome != GenerationOutcome.Succeeded || outcome.Poll is null)
                    throw new InvalidOperationException($"Generation returned no candidate: {outcome.Outcome} ({outcome.Reason}).");
                var generated = outcome.Poll;
                var decision = await qualityGate.EvaluateAsync(topic, generated, generated.Options);
                if (decision.Disposition == PollQualityDisposition.Rejected)
                    throw new InvalidOperationException($"Replacement rejected by publication gate: {string.Join(',', decision.ReasonCodes)}");
                var replacementId = await pollsRepository.CreateAsync(new CreatePollRequest
                {
                    Question = generated.Proposition, Description = topic.Summary, Category = generated.Category,
                    ExpiresAt = DateTime.UtcNow.AddHours(48), Options = generated.Options,
                    SourceType = topic.SourceType, SourceUrl = topic.SourceUrl, ThumbnailUrl = topic.ThumbnailUrl,
                    IsAIGenerated = true, GenerationMethod = generated.GenerationMethod,
                    TrendingTopicId = topic.Id, GenerationProvider = generated.GenerationProvider,
                    GenerationModel = generated.GenerationModel, QualityDecision = decision,
                    AutoPublish = decision.Disposition == PollQualityDisposition.Accepted,
                    ReplacementForCleanupRecordId = item.CleanupRecordId,
                    ModerationReason = string.Join(',', decision.ReasonCodes)
                });
                await cleanupRepository.CompleteRegenerationAsync(item, replacementId);
                logger.LogInformation("Regenerated malformed PollId={PollId} as ReplacementPollId={ReplacementPollId}", item.PollId, replacementId);
            }
            catch (Exception ex)
            {
                await cleanupRepository.FailRegenerationAsync(item, ex.Message);
                logger.LogError(ex, "Regeneration failed for PollId={PollId}; original remains hidden", item.PollId);
            }
        }
    }
}
