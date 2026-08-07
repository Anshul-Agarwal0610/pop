using BackendAPI.Models;

namespace BackendAPI.Services;

public static class PollBombRules
{
    public static bool IsApproved(PollBombOptions options, int target, int durationSeconds,
        PollBombExpiryPolicy policy) => options.AllowedThresholds.Contains(target)
        && options.AllowedDurationsSeconds.Contains(durationSeconds)
        && policy == PollBombExpiryPolicy.ExpireWithoutReveal;

    public static int Capacity(PollBombOptions options, int target) =>
        Math.Min(25, checked(target + Math.Max(0, options.CapacityAllowance)));

    public static bool IsExpired(DateTime expiresAt, DateTime now) => now >= expiresAt;
    public static bool ShouldReveal(LiveSessionStatus status, int validVotes, int target) =>
        status == LiveSessionStatus.Voting && validVotes >= target;
    public static bool CanRemove(LiveSessionStatus status) => status == LiveSessionStatus.Voting;
    public static bool ReminderEligible(bool optedIn, bool hasVoted, LiveSessionStatus status,
        DateTime expiresAt, DateTime now, DateTime? lastSentAt, int sentCount, PollBombOptions options) =>
        optedIn && !hasVoted && status == LiveSessionStatus.Voting && now < expiresAt
        && sentCount < options.MaximumReminders
        && (lastSentAt is null || now >= lastSentAt.Value.AddMinutes(options.ReminderCooldownMinutes));
}
