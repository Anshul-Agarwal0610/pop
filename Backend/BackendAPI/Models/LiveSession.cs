namespace BackendAPI.Models;

public enum LiveGameMode { Bomb }
public enum LiveSessionStatus { Voting, Revealed, Expired }
public enum PollBombExpiryPolicy { ExpireWithoutReveal }

public sealed class PollBombOptions
{
    public const string Section = "PollBomb";
    public int[] AllowedThresholds { get; set; } = [3, 5, 10, 20];
    public int[] AllowedDurationsSeconds { get; set; } = [900, 3600, 21600, 86400];
    public int CapacityAllowance { get; set; } = 5;
    public int ReminderCooldownMinutes { get; set; } = 60;
    public int MaximumReminders { get; set; } = 3;
}

public sealed record LiveSessionModeDto(string Mode, int[] AllowedThresholds, int[] AllowedDurationsSeconds,
    string[] ExpiryPolicies, int MaximumCapacity, bool AuthenticatedOnly);
public sealed record CreateLiveSessionRequest(LiveGameMode Mode, long PollId, int TargetVotes,
    int DurationSeconds, PollBombExpiryPolicy ExpiryPolicy, bool NotificationsEnabled = false);
public sealed record LockLiveSessionVoteRequest(long OptionId, string IdempotencyKey);
public sealed record SetLiveSessionNotificationsRequest(bool Enabled);

public sealed class LiveSessionStateDto
{
    public string PublicId { get; set; } = string.Empty;
    public LiveGameMode Mode { get; set; }
    public LiveSessionStatus Status { get; set; }
    public long HostUserId { get; set; }
    public long ParticipantId { get; set; }
    public bool IsHost { get; set; }
    public bool HasLockedVote { get; set; }
    public bool NotificationsEnabled { get; set; }
    public int JoinedCount { get; set; }
    public int LockedCount { get; set; }
    public int TargetVotes { get; set; }
    public int RemainingVotes => Math.Max(0, TargetVotes - LockedCount);
    public int StateVersion { get; set; }
    public DateTime ServerNow { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevealedAt { get; set; }
    public string? TerminalReason { get; set; }
    public LiveSessionPollDto Poll { get; set; } = new();
}

public sealed class LiveSessionPollDto
{
    public long Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<LiveSessionOptionDto> Options { get; set; } = [];
}

public sealed class LiveSessionOptionDto
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int? VoteCount { get; set; }
}

public sealed class LiveSessionEventDto
{
    public long Sequence { get; set; }
    public string Type { get; set; } = string.Empty;
    public int StateVersion { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}

public sealed class LiveSessionException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
