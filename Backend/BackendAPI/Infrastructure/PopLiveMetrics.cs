using System.Diagnostics.Metrics;
using BackendAPI.Analytics;
namespace BackendAPI.Infrastructure;

public sealed class PopLiveMetrics
{
    public const string MeterName = "Pollify.PopLive";
    private readonly Counter<long> _sessions;
    private readonly UpDownCounter<long> _activeSessions;
    private readonly Counter<long> _connections;
    private readonly UpDownCounter<long> _currentConnections;
    private readonly Counter<long> _failures;
    private readonly Counter<long> _rewards;
    public PopLiveMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(MeterName);
        _sessions = meter.CreateCounter<long>("pop_live.sessions");
        _activeSessions = meter.CreateUpDownCounter<long>("pop_live.sessions.active");
        _connections = meter.CreateCounter<long>("pop_live.signalr.connections");
        _currentConnections = meter.CreateUpDownCounter<long>("pop_live.signalr.connections.current");
        _failures = meter.CreateCounter<long>("pop_live.failures");
        _rewards = meter.CreateCounter<long>("pop_live.reward.decisions");
    }
    public void Session(string mode, string transition) { ValidateMode(mode); Require(transition, "created", "completed", "expired"); _sessions.Add(1, new("mode", mode), new("transition", transition)); _activeSessions.Add(transition == "created" ? 1 : -1, [new("mode", mode)]); }
    public void Connection(string mode, string hub, bool connected) { ValidateMode(mode); Require(hub, "poll_clash", "poll_bomb", "live_room"); _connections.Add(1, new("mode", mode), new("hub", hub), new("transition", connected ? "connected" : "disconnected")); _currentConnections.Add(connected ? 1 : -1, new("mode", mode), new("hub", hub)); }
    public void Failure(string mode, string code) { ValidateMode(mode); Require(code, "domain_rejected", "expired", "unavailable", "analytics_enqueue"); _failures.Add(1, new("mode", mode), new("code", code)); }
    public void Reward(string mode, string outcome) { ValidateMode(mode); Require(outcome, "allow", "cap", "hold", "suppress"); _rewards.Add(1, new("mode", mode), new("outcome", outcome)); }
    private static void ValidateMode(string mode) { if (!PopLiveAnalyticsContract.Modes.Contains(mode)) throw new ArgumentException("Unknown mode.", nameof(mode)); }
    private static void Require(string value, params string[] allowed) { if (!allowed.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("Unbounded metric label."); }
}
