using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IGeneratedPollCleanupClassifier
{
    GeneratedPollCleanupClassification Classify(GeneratedPollCleanupCandidate candidate);
}

public interface IGeneratedPollCleanupRepository
{
    Task<IReadOnlyList<GeneratedPollCleanupCandidate>> GetCandidatesAsync(long fromPollId, long toPollId, int maxRecords);
    Task<CleanupApplyResult> ApplyAsync(long pollId, Guid runId, string detectionVersion,
        IReadOnlyList<string> reasons, string generationSource);
    Task<IReadOnlyList<RegenerationQueueItem>> ClaimRegenerationBatchAsync(int maxRecords);
    Task<TrendingTopic?> ResolveTopicAsync(RegenerationQueueItem item);
    Task CompleteRegenerationAsync(RegenerationQueueItem item, long replacementPollId);
    Task FailRegenerationAsync(RegenerationQueueItem item, string error);
}

public interface IGeneratedPollCleanupService
{
    Task<GeneratedPollCleanupReport> DryRunAsync(long fromPollId, long toPollId, int maxRecords);
    Task<GeneratedPollCleanupReport> ExecuteAsync(long fromPollId, long toPollId, int maxRecords, Guid? runId = null);
}

