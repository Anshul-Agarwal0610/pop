using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using BackendAPI.Interfaces;
using BackendAPI.Models;

namespace BackendAPI.Observability;

public static class PipelineActivities
{
    public const string SourceName = "Pollify.Pipeline";
    public static readonly ActivitySource Source = new(SourceName);
    public static Activity? Start(string name, string? correlationId = null)
    {
        var activity = Source.StartActivity(name, ActivityKind.Internal);
        if (activity is not null && !string.IsNullOrWhiteSpace(correlationId))
            activity.SetTag("pipeline.correlation_id", correlationId);
        return activity;
    }
    public static string CorrelationId(Activity? activity = null) =>
        activity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
}

public sealed class PipelineMetrics : IDisposable
{
    public const string MeterName = "Pollify.Pipeline";
    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _ingestionTopics;
    private readonly Counter<long> _generationTopics;
    private readonly Counter<long> _llmRequests;
    private readonly Counter<long> _llmFailovers;
    private readonly Counter<long> _llmTokens;
    private readonly Histogram<double> _ingestionDuration;
    private readonly Histogram<double> _llmDuration;
    private int _paused;
    private long _queued;
    private long _retryPending;

    public PipelineMetrics()
    {
        _ingestionTopics = _meter.CreateCounter<long>("pollify.ingestion.topics");
        _generationTopics = _meter.CreateCounter<long>("pollify.generation.topics");
        _llmRequests = _meter.CreateCounter<long>("pollify.llm.requests");
        _llmFailovers = _meter.CreateCounter<long>("pollify.llm.failovers");
        _llmTokens = _meter.CreateCounter<long>("pollify.llm.tokens");
        _ingestionDuration = _meter.CreateHistogram<double>("pollify.ingestion.provider.duration", "ms");
        _llmDuration = _meter.CreateHistogram<double>("pollify.llm.request.duration", "ms");
        _meter.CreateObservableGauge("pollify.generation.paused", () => Volatile.Read(ref _paused));
        _meter.CreateObservableGauge("pollify.generation.backlog", () => new[] { new Measurement<long>(Interlocked.Read(ref _queued), Tags(("state","queued"))), new Measurement<long>(Interlocked.Read(ref _retryPending), Tags(("state","retry_pending"))) });
    }

    public void Ingestion(string stage, long count, string source) => _ingestionTopics.Add(count, Tags(("stage", stage), ("source", source)));
    public void Generation(string stage, long count = 1) => _generationTopics.Add(count, Tags(("stage", stage)));
    public void LlmRequest(string provider, string outcome) => _llmRequests.Add(1, Tags(("provider", provider), ("outcome", outcome)));
    public void Failover(string from, string to) => _llmFailovers.Add(1, Tags(("from_provider", from), ("to_provider", to)));
    public void Tokens(string provider, string type, long count) { if (count > 0) _llmTokens.Add(count, Tags(("provider", provider), ("type", type))); }
    public void IngestionDuration(string source, TimeSpan duration) => _ingestionDuration.Record(duration.TotalMilliseconds, Tags(("source", source)));
    public void LlmDuration(string provider, TimeSpan duration) => _llmDuration.Record(duration.TotalMilliseconds, Tags(("provider", provider)));
    public void UpdateGenerationState(bool paused, PipelineBacklog backlog) { Volatile.Write(ref _paused,paused?1:0); Interlocked.Exchange(ref _queued,backlog.Queued); Interlocked.Exchange(ref _retryPending,backlog.RetryPending); }
    private static TagList Tags(params (string Key, string Value)[] values) { var tags = new TagList(); foreach (var v in values) tags.Add(v.Key, v.Value); return tags; }
    public void Dispose() => _meter.Dispose();
}

public enum ProviderOperationalState { Enabled, Disabled, Misconfigured, CoolingDown }
public sealed record ProviderHealth(string Provider, ProviderOperationalState State, string? Reason, DateTimeOffset? LastAttempt, DateTimeOffset? LastSuccess, DateTimeOffset? LastRateLimit, DateTimeOffset? CooldownUntil, int LastItemCount = 0);

public interface IPipelineRuntimeHealth
{
    IReadOnlyCollection<ProviderHealth> Ingestion { get; }
    IReadOnlyCollection<ProviderHealth> Generation { get; }
    ProviderHealth GetGeneration(string provider, bool enabled, bool configured);
    void RecordIngestion(string source, ProviderOperationalState state, int count, bool success, bool rateLimited = false, string? reason = null, DateTimeOffset? cooldownUntil = null);
    void RecordGeneration(string provider, bool success, bool rateLimited = false, string? reason = null, DateTimeOffset? cooldownUntil = null);
}

public sealed class PipelineRuntimeHealth : IPipelineRuntimeHealth
{
    private readonly ConcurrentDictionary<string, ProviderHealth> _ingestion = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ProviderHealth> _generation = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<ProviderHealth> Ingestion => _ingestion.Values.OrderBy(x => x.Provider).ToArray();
    public IReadOnlyCollection<ProviderHealth> Generation => _generation.Values.OrderBy(x => x.Provider).ToArray();
    public ProviderHealth GetGeneration(string provider, bool enabled, bool configured)
    {
        if (_generation.TryGetValue(provider, out var value) && value.CooldownUntil > DateTimeOffset.UtcNow) return value with { State = ProviderOperationalState.CoolingDown };
        return value ?? new(provider, !enabled ? ProviderOperationalState.Disabled : configured ? ProviderOperationalState.Enabled : ProviderOperationalState.Misconfigured, !enabled ? "disabled" : configured ? null : "missing_configuration", null, null, null, null);
    }
    public void RecordIngestion(string source, ProviderOperationalState state, int count, bool success, bool rateLimited = false, string? reason = null, DateTimeOffset? cooldownUntil = null) =>
        _ingestion.AddOrUpdate(source, _ => New(source, state, count, success, rateLimited, reason, cooldownUntil), (_, old) => Merge(old, state, count, success, rateLimited, reason, cooldownUntil));
    public void RecordGeneration(string provider, bool success, bool rateLimited = false, string? reason = null, DateTimeOffset? cooldownUntil = null) =>
        _generation.AddOrUpdate(provider, _ => New(provider, cooldownUntil > DateTimeOffset.UtcNow ? ProviderOperationalState.CoolingDown : ProviderOperationalState.Enabled, 0, success, rateLimited, reason, cooldownUntil), (_, old) => Merge(old, cooldownUntil > DateTimeOffset.UtcNow ? ProviderOperationalState.CoolingDown : ProviderOperationalState.Enabled, 0, success, rateLimited, reason, cooldownUntil));
    private static ProviderHealth New(string name, ProviderOperationalState state, int count, bool success, bool limited, string? reason, DateTimeOffset? cooldown) { var now=DateTimeOffset.UtcNow; return new(name,state,reason,now,success?now:null,limited?now:null,cooldown,count); }
    private static ProviderHealth Merge(ProviderHealth old, ProviderOperationalState state, int count, bool success, bool limited, string? reason, DateTimeOffset? cooldown) { var now=DateTimeOffset.UtcNow; return old with { State=state, Reason=reason, LastAttempt=now, LastSuccess=success?now:old.LastSuccess, LastRateLimit=limited?now:old.LastRateLimit, CooldownUntil=cooldown, LastItemCount=count }; }
}
