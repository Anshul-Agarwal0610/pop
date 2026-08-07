using System.Text.Json;

namespace BackendAPI.Models;

public enum LiveGameMode { Clash, Relay, Bomb, Room }
public enum LiveSessionStatus { Lobby, Active, Completed, Expired, Abandoned }
public enum LiveRoundStatus { Pending, Active, Completed, Cancelled }
public enum LiveParticipantStatus { Joined, Ready, Active, Left, Removed }
public enum LiveSessionContentType { Poll, PollPack }

public sealed record LiveModeConfiguration(int MaxParticipants = 8, int RoundDurationSeconds = 60,
    int SessionDurationMinutes = 60, int? Lives = null, bool TeamPlay = false);

public sealed record CreateLiveSessionRequest(LiveGameMode Mode, LiveSessionContentType ContentType,
    long ContentId, LiveModeConfiguration Configuration);
public sealed record LiveVersionRequest(string Version);
public sealed record SubmitLiveResponseRequest(string Version, long OptionId);

public sealed class LiveSessionDto
{
    public long Id { get; init; }
    public long HostUserId { get; init; }
    public LiveGameMode Mode { get; init; }
    public LiveModeConfiguration Configuration { get; init; } = new();
    public LiveSessionContentType ContentType { get; init; }
    public long ContentId { get; init; }
    public LiveSessionStatus Status { get; init; }
    public string JoinCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? TerminalReason { get; init; }
    public string Version { get; init; } = string.Empty;
    public long LatestEventSequence { get; init; }
    public IReadOnlyList<LiveParticipantDto> Participants { get; init; } = [];
    public IReadOnlyList<LiveRoundDto> Rounds { get; init; } = [];
}

public sealed record LiveParticipantDto(long Id, long UserId, LiveParticipantStatus Status, DateTime JoinedAt);
public sealed record LiveRoundDto(long Id, int RoundNumber, long PollId, LiveRoundStatus Status,
    DateTime? StartsAt, DateTime? EndsAt, DateTime? CompletedAt);
public sealed record LiveResponseDto(long Id, long RoundId, long ParticipantId, long PollId, long OptionId, DateTime SubmittedAt);
public sealed record LiveSessionEventDto(long Sequence, string Type, long? ActorUserId, JsonElement Payload,
    int SchemaVersion, DateTime OccurredAt);
public sealed record LiveEventReplayDto(string SessionVersion, long LatestSequence, IReadOnlyList<LiveSessionEventDto> Events);
public sealed record LiveCleanupResult(int Expired, int Abandoned);

public sealed class LiveSessionException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
