using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class LiveSessionRulesTests
{
    [Fact]
    public void Reveal_is_scheduled_only_after_every_eligible_member_locks()
    {
        Assert.False(LiveSessionRules.ShouldScheduleReveal(LiveSessionStatuses.Voting, 2, 1));
        Assert.True(LiveSessionRules.ShouldScheduleReveal(LiveSessionStatuses.Voting, 2, 2));
        Assert.False(LiveSessionRules.ShouldScheduleReveal(LiveSessionStatuses.Revealed, 2, 2));
        Assert.False(LiveSessionRules.ShouldScheduleReveal(LiveSessionStatuses.Voting, 0, 0));
    }

    [Fact]
    public void Reveal_deadline_is_derived_from_server_time()
    {
        var now = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now.AddSeconds(2), LiveSessionRules.RevealDeadline(now, TimeSpan.FromSeconds(2)));
        Assert.Equal(now, LiveSessionRules.RevealDeadline(now, TimeSpan.FromSeconds(-1)));
    }

    [Theory]
    [InlineData("Lobby", false)]
    [InlineData("Voting", false)]
    [InlineData("Revealed", true)]
    [InlineData("Completed", true)]
    public void Results_are_exposed_only_after_a_persisted_reveal(string status, bool expected)
    {
        var revealedAt = new DateTime(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, LiveSessionRules.CanExposeResults(status, revealedAt));
        Assert.False(LiveSessionRules.CanExposeResults(status, null));
    }

    [Fact]
    public void State_versions_reject_duplicates_and_out_of_order_events()
    {
        Assert.True(LiveSessionRules.IsNewer(4, 5));
        Assert.False(LiveSessionRules.IsNewer(5, 5));
        Assert.False(LiveSessionRules.IsNewer(6, 5));
    }
}
