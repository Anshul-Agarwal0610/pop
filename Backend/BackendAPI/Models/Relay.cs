namespace BackendAPI.Models;

public static class RelayErrorCodes
{
    public const string Expired = "handoff_expired";
    public const string Replayed = "handoff_replayed";
    public const string BranchConflict = "relay_branch_conflict";
    public const string CycleDetected = "relay_cycle_detected";
    public const string Blocked = "relay_blocked";
    public const string Invalid = "relay_invalid";
    public const string Forbidden = "relay_forbidden";
}

public sealed class RelayDomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public static class RelayRules
{
    public const int MinimumTtlMinutes = 5;
    public const int MaximumTtlMinutes = 10_080;
    public const int MinimumLength = 2;
    public const int MaximumLength = 100;

    public static int ValidateTtl(int minutes, int minimum = MinimumTtlMinutes, int maximum = MaximumTtlMinutes)
    {
        if (minutes < minimum || minutes > maximum) throw new ArgumentOutOfRangeException(nameof(minutes));
        return minutes;
    }

    public static int ValidateLength(int length)
    {
        if (length < MinimumLength || length > MaximumLength) throw new ArgumentOutOfRangeException(nameof(length));
        return length;
    }

    public static DateTime Deadline(DateTime utcNow, int ttlMinutes)
    {
        if (utcNow.Kind != DateTimeKind.Utc) throw new ArgumentException("Deadline requires UTC.", nameof(utcNow));
        return utcNow.AddMinutes(ValidateTtl(ttlMinutes));
    }

    public static int? NextMilestone(int chainLength, IEnumerable<int> milestones) =>
        milestones.Where(x => x > chainLength).OrderBy(x => x).Cast<int?>().FirstOrDefault();

    public static bool IsTerminal(int chainLength, int maxLength, bool stopRequested) =>
        stopRequested || chainLength >= maxLength;

    public static void EnsureDifferentUsers(long senderId, long receiverId)
    {
        if (senderId == receiverId) throw new RelayDomainException(RelayErrorCodes.CycleDetected, "A relay cannot be handed to yourself.");
    }
}

public sealed record StartRelayRequest(long PollId, int HandoffTtlMinutes = 1440, int MaxLength = 10, string TransferMethod = "Link");
public sealed record CompleteRelayRequest(long OptionId, bool ReceiveFinalOutcome, bool EndChain = false, string? NextTransferMethod = "Link", string? IdempotencyKey = null);
public sealed record RelayConsentRequest(bool ReceiveFinalOutcome);
public sealed record RelayStartResult(long ChainId, string HandoffToken, DateTime ExpiresAt, DateTime ServerNow);
public sealed record RelayCompleteResult(long ChainId, string Status, int ChainLength, int? NextMilestone, string? HandoffToken, DateTime? ExpiresAt, bool RewardCapped, bool RewardEligible, DateTime ServerNow);
public sealed class RelayHandoffView
{
    public long ChainId { get; init; }
    public long PollId { get; init; }
    public string Question { get; init; } = string.Empty;
    public IReadOnlyList<PollOption> Options { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public int ChainLength { get; init; }
    public int? NextMilestone { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime ServerNow { get; init; }
    public bool CanAccept { get; init; }
    public bool IsAcceptedByCurrentUser { get; init; }
}
public sealed class RelayProgress
{
    public long ChainId { get; init; }
    public long PollId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ChainLength { get; init; }
    public int MaxLength { get; init; }
    public int? NextMilestone { get; init; }
    public DateTime? CurrentDeadline { get; init; }
    public DateTime ServerNow { get; init; }
    public bool ReceiveFinalOutcome { get; init; }
}
public sealed record RelayOutcome(long ChainId, int TotalVotes, IReadOnlyList<RelayOutcomeOption> Options, DateTime FinalizedAt);
public sealed record RelayOutcomeOption(long OptionId, string Text, int VoteCount, double VotePercentage);
