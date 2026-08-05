using BackendAPI.Models;

namespace BackendAPI.Services;

public sealed record LeaderboardEvent(
    long UserId, string Username, long Amount, DateTime OccurredAt,
    bool IsValid = true, bool IsLeaderboardEligible = true);

public sealed record RankedUser(long UserId, string Username, long PeriodXp, long Rank);

/// <summary>In-memory expression of the same eligibility, competition-rank, and tie-order rules used by the SQL query.</summary>
public static class LeaderboardRanking
{
    public static IReadOnlyList<RankedUser> Rank(
        IEnumerable<LeaderboardEvent> events, LeaderboardWindow window)
    {
        var totals = events
            .Where(e => e.IsValid && e.IsLeaderboardEligible && e.Amount > 0)
            .Where(e => window.StartUtc == null || e.OccurredAt >= window.StartUtc)
            .Where(e => window.EndUtc == null || e.OccurredAt < window.EndUtc)
            .GroupBy(e => new { e.UserId, e.Username })
            .Select(g => new { g.Key.UserId, g.Key.Username, Xp = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Xp)
            .ThenBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.UserId)
            .ToList();

        return totals.Select((item, index) => new RankedUser(
            item.UserId, item.Username, item.Xp,
            totals.FindIndex(x => x.Xp == item.Xp) + 1)).ToList();
    }
}
