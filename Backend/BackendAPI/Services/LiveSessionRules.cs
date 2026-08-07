using BackendAPI.Models;

namespace BackendAPI.Services;

public static class LiveSessionRules
{
    public static bool ShouldScheduleReveal(string status, int eligibleCount, int lockedCount) =>
        status == LiveSessionStatuses.Voting && eligibleCount > 0 && lockedCount >= eligibleCount;

    public static DateTime RevealDeadline(DateTime serverNow, TimeSpan delay) =>
        serverNow.Add(delay < TimeSpan.Zero ? TimeSpan.Zero : delay);

    public static bool CanExposeResults(string status, DateTime? revealedAt) =>
        status is LiveSessionStatuses.Revealed or LiveSessionStatuses.Completed && revealedAt.HasValue;

    public static bool IsNewer(long currentVersion, long incomingVersion) => incomingVersion > currentVersion;
}
