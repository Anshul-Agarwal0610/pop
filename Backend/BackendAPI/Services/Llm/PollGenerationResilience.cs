using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;

namespace BackendAPI.Services.Llm;

public interface IJitterSource { double Next(); }
public sealed class RandomJitterSource : IJitterSource { public double Next() => Random.Shared.NextDouble(); }

public interface IRetryDelayPolicy
{
    DateTimeOffset GetNextAttempt(int attempt, DateTimeOffset now, DateTimeOffset? providerRetryAt = null);
}

public sealed class RetryDelayPolicy(IOptions<PollGenerationOptions> options, IJitterSource jitter) : IRetryDelayPolicy
{
    public DateTimeOffset GetNextAttempt(int attempt, DateTimeOffset now, DateTimeOffset? providerRetryAt = null)
    {
        var o = options.Value;
        var exponent = Math.Min(30, Math.Max(0, attempt - 1));
        var capped = Math.Min(o.MaxRetryDelaySeconds, o.BaseRetryDelaySeconds * Math.Pow(2, exponent));
        var delay = capped + capped * o.JitterPercentage * Math.Clamp(jitter.Next(), 0, 1);
        var application = now.AddSeconds(delay);
        return providerRetryAt > application ? providerRetryAt.Value : application;
    }
}

public static class LlmHttpFailureClassifier
{
    public static LlmFailureClass Classify(HttpStatusCode status, string? body)
    {
        var text = body ?? string.Empty;
        if (text.Contains("content_policy", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("safety", StringComparison.OrdinalIgnoreCase)) return LlmFailureClass.ContentPolicy;
        return (int)status switch
        {
            429 => LlmFailureClass.RateLimited,
            408 or 500 or 502 or 503 or 504 => LlmFailureClass.TransientServer,
            401 or 403 => LlmFailureClass.Authentication,
            400 or 404 or 422 => LlmFailureClass.InvalidRequest,
            _ => LlmFailureClass.Unknown
        };
    }

    public static DateTimeOffset? GetRetryAt(HttpResponseMessage response, DateTimeOffset now, TimeSpan maximum)
    {
        var candidates = new List<DateTimeOffset>();
        var retry = response.Headers.RetryAfter;
        if (retry?.Delta is { } delta) candidates.Add(now + delta);
        if (retry?.Date is { } date) candidates.Add(date);
        foreach (var name in new[] { "x-ratelimit-reset-requests", "x-ratelimit-reset-tokens",
            "anthropic-ratelimit-requests-reset", "anthropic-ratelimit-tokens-reset", "RateLimit-Reset", "X-RateLimit-Reset" })
        {
            IEnumerable<string>? values = null;
            if (!response.Headers.TryGetValues(name, out values)) response.Content?.Headers.TryGetValues(name, out values);
            foreach (var value in values ?? Array.Empty<string>())
                if (TryParseReset(value, now, out var parsed)) candidates.Add(parsed);
        }
        var latest = candidates.Where(x => x > now).DefaultIfEmpty().Max();
        return latest == default ? null : (latest > now + maximum ? now + maximum : latest);
    }

    private static bool TryParseReset(string value, DateTimeOffset now, out DateTimeOffset result)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result)) return true;
        if (double.TryParse(value.TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            result = number > 1_000_000_000 ? DateTimeOffset.FromUnixTimeSeconds((long)number) : now.AddSeconds(number);
            return true;
        }
        result = default; return false;
    }
}

public interface IProviderResilienceCoordinator
{
    Task<IAsyncDisposable?> TryAcquireAsync(string provider, CancellationToken ct);
    void RecordSuccess(string provider);
    void RecordFailure(string provider, LlmProviderResult result);
}

public sealed class ProviderResilienceCoordinator(IOptions<PollGenerationOptions> options, TimeProvider time)
    : IProviderResilienceCoordinator
{
    private sealed class State(int concurrency) { public int Failures; public DateTimeOffset OpenUntil; public int Probe; public readonly SemaphoreSlim Slots = new(concurrency); }
    private readonly ConcurrentDictionary<string, State> _states = new(StringComparer.OrdinalIgnoreCase);

    public async Task<IAsyncDisposable?> TryAcquireAsync(string provider, CancellationToken ct)
    {
        var state = _states.GetOrAdd(provider, _ => new State(Math.Max(1, options.Value.MaxProviderConcurrency)));
        var now = time.GetUtcNow();
        var probe = false;
        if (state.OpenUntil > now) return null;
        if (state.OpenUntil != default && Interlocked.CompareExchange(ref state.Probe, 1, 0) != 0) return null;
        if (state.OpenUntil != default) probe = true;
        await state.Slots.WaitAsync(ct);
        return new Releaser(state, probe);
    }

    public void RecordSuccess(string provider)
    {
        var s = _states.GetOrAdd(provider, _ => new State(Math.Max(1, options.Value.MaxProviderConcurrency)));
        s.Failures = 0; s.OpenUntil = default; Interlocked.Exchange(ref s.Probe, 0);
    }

    public void RecordFailure(string provider, LlmProviderResult result)
    {
        var s = _states.GetOrAdd(provider, _ => new State(Math.Max(1, options.Value.MaxProviderConcurrency)));
        Interlocked.Exchange(ref s.Probe, 0);
        if (!result.IsRetryable) return;
        if (Interlocked.Increment(ref s.Failures) >= options.Value.CircuitFailureThreshold)
            s.OpenUntil = result.RetryAtUtc > time.GetUtcNow()
                ? result.RetryAtUtc.Value : time.GetUtcNow().AddSeconds(options.Value.CircuitCooldownSeconds);
    }

    private sealed class Releaser(State state, bool probe) : IAsyncDisposable
    { public ValueTask DisposeAsync() { state.Slots.Release(); if (probe) Interlocked.Exchange(ref state.Probe, 0); return ValueTask.CompletedTask; } }
}
