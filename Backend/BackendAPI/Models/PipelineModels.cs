namespace BackendAPI.Models;

public static class TopicProcessingStatus
{
    public const string Queued="Queued", Processing="Processing", RetryPending="RetryPending", Converted="Converted", Published="Published", Review="Review", Rejected="Rejected";
}

public sealed record TopicSaveResult(int Submitted, int Inserted, int Deduplicated);
public sealed record PipelineBacklog(int Queued, int Processing, int RetryPending, DateTime? OldestEligibleAt);
public sealed record PipelineControlState(bool GenerationPaused, DateTime UpdatedAt, string? UpdatedBy);

public enum IngestionSourceState { Enabled, Disabled, Misconfigured, CoolingDown, Failed }
public sealed record IngestionFetchResult(string Source, IngestionSourceState State, IReadOnlyList<TrendingTopic> Topics, int RequestCount, int SuccessCount, int RateLimitCount, TimeSpan Duration, string? ErrorCode = null)
{
    public static IngestionFetchResult Disabled(string source) => new(source, IngestionSourceState.Disabled, [], 0, 0, 0, TimeSpan.Zero, "disabled");
    public static IngestionFetchResult Misconfigured(string source) => new(source, IngestionSourceState.Misconfigured, [], 0, 0, 0, TimeSpan.Zero, "missing_configuration");
}
