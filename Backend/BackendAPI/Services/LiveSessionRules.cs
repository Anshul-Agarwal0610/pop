using BackendAPI.Models;

namespace BackendAPI.Services;

public static class LiveSessionRules
{
    public static readonly TimeSpan AbandonmentThreshold = TimeSpan.FromMinutes(15);

    public static void Validate(LiveGameMode mode, LiveModeConfiguration config)
    {
        if (config.MaxParticipants is < 2 or > 100 || config.RoundDurationSeconds is < 10 or > 300 ||
            config.SessionDurationMinutes is < 5 or > 240)
            throw new LiveSessionException("invalid_mode_config", "Participant, round, or session limits are invalid.");
        if (mode == LiveGameMode.Bomb && config.Lives is not (>= 1 and <= 10))
            throw new LiveSessionException("invalid_mode_config", "Bomb requires between 1 and 10 lives.");
        if (mode != LiveGameMode.Bomb && config.Lives is not null)
            throw new LiveSessionException("invalid_mode_config", "Lives are only supported by Bomb.");
        if (mode == LiveGameMode.Relay && !config.TeamPlay)
            throw new LiveSessionException("invalid_mode_config", "Relay requires team play.");
        if (mode != LiveGameMode.Relay && config.TeamPlay)
            throw new LiveSessionException("invalid_mode_config", "Team play is only supported by Relay.");
    }

    public static bool IsTerminal(LiveSessionStatus status) => status is LiveSessionStatus.Completed or LiveSessionStatus.Expired or LiveSessionStatus.Abandoned;
    public static bool IsExpired(DateTime expiresAt, DateTime now) => now >= expiresAt;
    public static bool IsAbandoned(DateTime lastActivityAt, DateTime now) => now - lastActivityAt >= AbandonmentThreshold;
    public static bool CanTransition(LiveSessionStatus from, LiveSessionStatus to) => (from, to) switch
    {
        (LiveSessionStatus.Lobby, LiveSessionStatus.Active or LiveSessionStatus.Expired or LiveSessionStatus.Abandoned) => true,
        (LiveSessionStatus.Active, LiveSessionStatus.Completed or LiveSessionStatus.Expired or LiveSessionStatus.Abandoned) => true,
        _ => false
    };
    public static void RequireTransition(LiveSessionStatus from, LiveSessionStatus to)
    {
        if (!CanTransition(from, to)) throw new LiveSessionException("invalid_transition", $"Cannot transition from {from} to {to}.");
    }
    public static string CompletionRewardSource(long sessionId, long participantId) => $"live-session:{sessionId}:completion:{participantId}";
    public static string RoundWinnerRewardSource(long sessionId, long roundId, long participantId) => $"live-session:{sessionId}:round:{roundId}:winner:{participantId}";
}
