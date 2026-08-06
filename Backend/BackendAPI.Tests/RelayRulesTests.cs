using BackendAPI.Models;
using Xunit;

namespace BackendAPI.Tests;

public sealed class RelayRulesTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(10081)]
    public void Ttl_is_accessible_but_bounded(int minutes) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RelayRules.ValidateTtl(minutes));

    [Fact]
    public void Deadline_uses_utc_and_configured_minutes()
    {
        var now=new DateTime(2026,8,7,10,0,0,DateTimeKind.Utc);
        Assert.Equal(now.AddHours(24),RelayRules.Deadline(now,1440));
        Assert.Throws<ArgumentException>(()=>RelayRules.Deadline(DateTime.SpecifyKind(now,DateTimeKind.Local),60));
    }

    [Theory]
    [InlineData(0,3)]
    [InlineData(3,5)]
    [InlineData(10,25)]
    public void Next_milestone_never_reveals_votes(int length,int expected) =>
        Assert.Equal(expected,RelayRules.NextMilestone(length,[3,5,10,25]));

    [Fact]
    public void Terminal_at_maximum_or_explicit_stop()
    {
        Assert.True(RelayRules.IsTerminal(10,10,false));
        Assert.True(RelayRules.IsTerminal(2,10,true));
        Assert.False(RelayRules.IsTerminal(9,10,false));
    }

    [Fact]
    public void Self_handoff_is_detected_with_stable_code()
    {
        var ex=Assert.Throws<RelayDomainException>(()=>RelayRules.EnsureDifferentUsers(7,7));
        Assert.Equal(RelayErrorCodes.CycleDetected,ex.Code);
    }
}
