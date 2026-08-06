using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class LeaderboardTests
{
    private static readonly DateTime Monday = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void WeeklyWindow_IsMondayInclusiveAndNextMondayExclusive()
    {
        var window = LeaderboardWindow.For(LeaderboardPeriod.Weekly, Monday.AddDays(3));
        Assert.Equal(Monday, window.StartUtc);
        Assert.Equal(Monday.AddDays(7), window.EndUtc);

        var ranked = LeaderboardRanking.Rank(new[] {
            new LeaderboardEvent(1, "at-start", 10, Monday),
            new LeaderboardEvent(2, "before", 20, Monday.AddTicks(-1)),
            new LeaderboardEvent(3, "at-end", 30, Monday.AddDays(7))
        }, window);
        Assert.Collection(ranked, row => Assert.Equal(1, row.UserId));
    }

    [Fact]
    public void CompetitionRanks_ShareAndSkipRanks_WithStableTieOrder()
    {
        var events = new[] {
            new LeaderboardEvent(1, "zeta", 100, Monday),
            new LeaderboardEvent(3, "charlie", 80, Monday),
            new LeaderboardEvent(2, "Alpha", 80, Monday),
            new LeaderboardEvent(4, "delta", 60, Monday)
        };
        var rows = LeaderboardRanking.Rank(events, new(null, null));
        Assert.Equal(new long[] { 1, 2, 3, 4 }, rows.Select(x => x.UserId));
        Assert.Equal(new long[] { 1, 2, 2, 4 }, rows.Select(x => x.Rank));
    }

    [Fact]
    public void PeriodXp_IsEventSum_AndInvalidOrPrivateEventsAreExcluded()
    {
        var rows = LeaderboardRanking.Rank(new[] {
            new LeaderboardEvent(1, "valid", 20, Monday),
            new LeaderboardEvent(1, "valid", 30, Monday.AddHours(1)),
            new LeaderboardEvent(1, "valid", 1000, Monday, IsValid: false),
            new LeaderboardEvent(2, "private", 500, Monday, IsLeaderboardEligible: false)
        }, new(null, null));
        var row = Assert.Single(rows);
        Assert.Equal(50, row.PeriodXp);
        Assert.Equal(1, row.UserId);
    }

    [Fact]
    public void EmptyPeriod_ReturnsNoRows()
    {
        var window = LeaderboardWindow.For(LeaderboardPeriod.Weekly, Monday);
        Assert.Empty(LeaderboardRanking.Rank(Array.Empty<LeaderboardEvent>(), window));
    }
}
