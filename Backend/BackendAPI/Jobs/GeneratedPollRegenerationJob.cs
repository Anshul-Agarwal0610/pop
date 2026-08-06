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
                var topic = await cleanupRepository.ResolveTopicAsync(item);
                if (topic is null) throw new InvalidOperationException("No unique trending topic provenance; manual review is required.");
                var generated = await generator.GenerateAsync(topic);
                if (generated is null) throw new InvalidOperationException("Generation returned no candidate.");
                var decision = await qualityGate.EvaluateAsync(topic, generated, GeneratedPollContract.CanonicalOptions);
                if (decision.Disposition == PollQualityDisposition.Rejected)
                    throw new InvalidOperationException($"Replacement rejected by publication gate: {string.Join(',', decision.ReasonCodes)}");
                var replacementId = await pollsRepository.CreateAsync(new CreatePollRequest
                {
                    Question = generated.Proposition, Description = topic.Summary, Category = generated.Category,
                    ExpiresAt = DateTime.UtcNow.AddHours(48), Options = GeneratedPollContract.CanonicalOptions.ToList(),
                    SourceType = topic.SourceType, SourceUrl = topic.SourceUrl, ThumbnailUrl = topic.ThumbnailUrl,
                    IsAIGenerated = true, TrendingTopicId = topic.Id, QualityDecision = decision,
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
