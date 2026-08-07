namespace BackendAPI.Models;

public static class LiveSessionStatuses
{
    public const string Lobby = "Lobby";
    public const string Voting = "Voting";
    public const string Revealed = "Revealed";
    public const string Completed = "Completed";
}

public sealed record LiveParticipantDto(long UserId, string DisplayName, bool IsReady, bool IsLocked);
public sealed record LiveOptionDto(long Id, string Text, int? VoteCount);
public sealed record LiveRevealDto(long? WinningOptionId, IReadOnlyList<LiveOptionDto> Options);

public sealed class LiveSessionStateDto
{
    public Guid SessionId { get; init; }
    public string Status { get; init; } = string.Empty;
    public long StateVersion { get; init; }
    public DateTime ServerNow { get; init; }
    public int CurrentRound { get; init; }
    public long? PollId { get; init; }
    public string? Question { get; init; }
    public DateTime? RevealAt { get; init; }
    public int EligibleCount { get; init; }
    public int LockedCount { get; init; }
    public long? MyOptionId { get; init; }
    public IReadOnlyList<LiveParticipantDto> Participants { get; init; } = [];
    public IReadOnlyList<LiveOptionDto> Options { get; init; } = [];
    public LiveRevealDto? Reveal { get; init; }
}

public sealed class LiveVoteRequest
{
    public long OptionId { get; init; }
    public Guid IdempotencyKey { get; init; }
}

public sealed record LiveReadyRequest(bool IsReady);
public sealed record LiveVoteResult(LiveSessionStateDto State, bool WasDuplicate, bool RevealScheduled);

public sealed record LiveSessionEvent(
    string Type, Guid SessionId, long StateVersion, DateTime ServerNow, DateTime? RevealAt = null);

public sealed class LiveSessionException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
