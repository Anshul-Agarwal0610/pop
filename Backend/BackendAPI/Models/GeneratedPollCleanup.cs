namespace BackendAPI.Models;

public static class GeneratedPollCleanupPolicy
{
    public const string DetectionVersion = "malformed-generated-poll-v1";
    public const string DeactivateAndRegenerate = "DeactivateAndRegenerate";
    public const string PreserveAndHide = "PreserveAndHide";
    public const int MaximumBatchSize = 500;
}

public static class GeneratedPollCleanupReasons
{
    public const string OptionCardinality = "options.cardinality";
    public const string OptionText = "options.noncanonical_text";
    public const string OptionSide = "options.invalid_side";
    public const string HistoricalFallback = "question.historical_fallback";
    public const string SurveyFraming = "question.survey_framing";
}

public class GeneratedPollCleanupCandidate
{
    public long PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool IsAIGenerated { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrending { get; set; }
    public string? SourceType { get; set; }
    public string? SourceUrl { get; set; }
    public long VoteCount { get; set; }
    public long? TrendingTopicId { get; set; }
    public string? GenerationProvider { get; set; }
    public string? CleanupStatus { get; set; }
    public List<PollOption> Options { get; set; } = [];
}

public sealed record GeneratedPollCleanupClassification(
    bool IsMalformed, string DetectionVersion, IReadOnlyList<string> Reasons,
    string GenerationSource, string ProposedDisposition);

public sealed class GeneratedPollCleanupSample
{
    public long PollId { get; init; }
    public string Question { get; init; } = string.Empty;
    public IReadOnlyList<string> Options { get; init; } = [];
    public long VoteCount { get; init; }
    public string? SourceUrl { get; init; }
    public string IngestionSource { get; init; } = "unknown";
    public string GenerationSource { get; init; } = "legacy-unknown";
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public string ProposedDisposition { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? ExistingCleanupStatus { get; init; }
}

public sealed record GeneratedPollCleanupGroup(string Reason, string VoteClass, string GenerationSource,
    string IngestionSource, bool IsActive, string CleanupStatus, int Count);

public sealed class GeneratedPollCleanupReport
{
    public Guid RunId { get; init; }
    public long FromPollId { get; init; }
    public long ToPollId { get; init; }
    public int ScannedCount { get; init; }
    public int MalformedCount { get; init; }
    public int ChangedCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<GeneratedPollCleanupGroup> Groups { get; init; } = [];
    public IReadOnlyList<GeneratedPollCleanupSample> Sample { get; init; } = [];
}

public sealed class GeneratedPollCleanupRequest
{
    public long FromPollId { get; set; }
    public long ToPollId { get; set; }
    public int MaxRecords { get; set; }
    public bool DryRun { get; set; } = true;
    public string? Confirmation { get; set; }
}

public sealed record CleanupApplyResult(bool Changed, string Disposition, string Status, string? Error = null);
public sealed record RegenerationQueueItem(long CleanupRecordId, long PollId, long? TrendingTopicId,
    string? SourceUrl, int AttemptCount, long? ReplacementPollId = null);
