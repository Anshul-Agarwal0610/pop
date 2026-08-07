using BackendAPI.Interfaces;
using BackendAPI.Models;

namespace BackendAPI.Services;

public sealed class GeneratedPollCleanupService(
    IGeneratedPollCleanupRepository repository,
    IGeneratedPollCleanupClassifier classifier,
    ILogger<GeneratedPollCleanupService> logger) : IGeneratedPollCleanupService
{
    public Task<GeneratedPollCleanupReport> DryRunAsync(long fromPollId, long toPollId, int maxRecords) =>
        RunAsync(fromPollId, toPollId, maxRecords, true, Guid.NewGuid());

    public Task<GeneratedPollCleanupReport> ExecuteAsync(long fromPollId, long toPollId, int maxRecords, Guid? runId = null) =>
        RunAsync(fromPollId, toPollId, maxRecords, false, runId ?? Guid.NewGuid());

    private async Task<GeneratedPollCleanupReport> RunAsync(long from, long to, int max, bool dryRun, Guid runId)
    {
        ValidateBounds(from, to, max);
        var candidates = await repository.GetCandidatesAsync(from, to, max);
        var malformed = candidates.Select(x => (Candidate: x, Classification: classifier.Classify(x)))
            .Where(x => x.Classification.IsMalformed).ToList();
        var changed = 0; var failed = 0;
        if (!dryRun)
        {
            foreach (var item in malformed)
            {
                try
                {
                    var result = await repository.ApplyAsync(item.Candidate.PollId, runId,
                        item.Classification.DetectionVersion, item.Classification.Reasons, item.Classification.GenerationSource);
                    if (result.Changed) changed++;
                    if (result.Error is not null) failed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogError(ex, "Cleanup failed for PollId={PollId} RunId={RunId}", item.Candidate.PollId, runId);
                }
            }
        }
        var samples = malformed.Take(50).Select(x => new GeneratedPollCleanupSample
        {
            PollId = x.Candidate.PollId, Question = x.Candidate.Question,
            Options = x.Candidate.Options.Select(o => o.Side is null ? o.Text : $"{o.Text} [{o.Side}]").ToArray(),
            VoteCount = x.Candidate.VoteCount, SourceUrl = x.Candidate.SourceUrl,
            IngestionSource = x.Candidate.SourceType ?? "unknown", GenerationSource = x.Classification.GenerationSource,
            Reasons = x.Classification.Reasons, ProposedDisposition = x.Classification.ProposedDisposition,
            IsActive = x.Candidate.IsActive, ExistingCleanupStatus = x.Candidate.CleanupStatus
        }).ToArray();
        var groups = malformed.SelectMany(x => x.Classification.Reasons.Select(reason => new
        {
            Reason = reason, VoteClass = x.Candidate.VoteCount == 0 ? "unvoted" : "voted",
            x.Classification.GenerationSource, Ingestion = x.Candidate.SourceType ?? "unknown",
            x.Candidate.IsActive, Status = x.Candidate.CleanupStatus ?? "none"
        })).GroupBy(x => new { x.Reason, x.VoteClass, x.GenerationSource, x.Ingestion, x.IsActive, x.Status })
          .Select(g => new GeneratedPollCleanupGroup(g.Key.Reason, g.Key.VoteClass, g.Key.GenerationSource,
              g.Key.Ingestion, g.Key.IsActive, g.Key.Status, g.Count())).ToArray();
        logger.LogInformation("Generated poll cleanup RunId={RunId} DryRun={DryRun} Bounds={From}-{To} Scanned={Scanned} Malformed={Malformed} Changed={Changed} Failed={Failed}",
            runId, dryRun, from, to, candidates.Count, malformed.Count, changed, failed);
        return new() { RunId = runId, FromPollId = from, ToPollId = to, ScannedCount = candidates.Count,
            MalformedCount = malformed.Count, ChangedCount = changed, FailedCount = failed, Groups = groups, Sample = samples };
    }

    public static void ValidateBounds(long from, long to, int max)
    {
        if (from <= 0 || to < from) throw new ArgumentOutOfRangeException(nameof(from), "A positive bounded poll ID range is required.");
        if (max is <= 0 or > GeneratedPollCleanupPolicy.MaximumBatchSize)
            throw new ArgumentOutOfRangeException(nameof(max), $"maxRecords must be between 1 and {GeneratedPollCleanupPolicy.MaximumBatchSize}.");
    }
}
