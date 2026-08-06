using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class PollBombRulesTests
{
    private readonly PollBombOptions options = new();

    [Theory]
    [InlineData(3,900)] [InlineData(5,3600)] [InlineData(10,21600)] [InlineData(20,86400)]
    public void Approved_configurations_are_accepted(int target,int duration) =>
        Assert.True(PollBombRules.IsApproved(options,target,duration,PollBombExpiryPolicy.ExpireWithoutReveal));

    [Theory]
    [InlineData(2,900)] [InlineData(100,900)] [InlineData(3,899)] [InlineData(3,86401)]
    public void Arbitrary_configurations_are_rejected(int target,int duration) =>
        Assert.False(PollBombRules.IsApproved(options,target,duration,PollBombExpiryPolicy.ExpireWithoutReveal));

    [Fact]
    public void Capacity_is_bounded_for_modest_rooms()
    {
        Assert.Equal(8,PollBombRules.Capacity(options,3));
        Assert.Equal(25,PollBombRules.Capacity(options,20));
    }

    [Fact]
    public void Expiry_boundary_is_inclusive()
    {
        var expiry=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
        Assert.False(PollBombRules.IsExpired(expiry,expiry.AddTicks(-1)));
        Assert.True(PollBombRules.IsExpired(expiry,expiry));
    }

    [Theory]
    [InlineData(2,false)] [InlineData(3,true)] [InlineData(4,true)]
    public void Reveal_requires_fixed_threshold(int votes,bool expected) =>
        Assert.Equal(expected,PollBombRules.ShouldReveal(LiveSessionStatus.Voting,votes,3));

    [Theory]
    [InlineData(LiveSessionStatus.Revealed)] [InlineData(LiveSessionStatus.Expired)]
    public void Terminal_states_never_reveal_again(LiveSessionStatus status) =>
        Assert.False(PollBombRules.ShouldReveal(status,100,3));

    [Fact]
    public void Removal_is_only_allowed_before_reveal()
    {
        Assert.True(PollBombRules.CanRemove(LiveSessionStatus.Voting));
        Assert.False(PollBombRules.CanRemove(LiveSessionStatus.Revealed));
    }

    [Fact]
    public void Reminders_require_opt_in_and_respect_cooldown_and_limit()
    {
        var now=DateTime.UtcNow;
        Assert.False(PollBombRules.ReminderEligible(false,false,LiveSessionStatus.Voting,now.AddHours(1),now,null,0,options));
        Assert.False(PollBombRules.ReminderEligible(true,false,LiveSessionStatus.Voting,now.AddHours(1),now,now.AddMinutes(-59),0,options));
        Assert.False(PollBombRules.ReminderEligible(true,false,LiveSessionStatus.Voting,now.AddHours(1),now,null,3,options));
        Assert.False(PollBombRules.ReminderEligible(true,false,LiveSessionStatus.Expired,now.AddHours(1),now,null,0,options));
        Assert.True(PollBombRules.ReminderEligible(true,false,LiveSessionStatus.Voting,now.AddHours(1),now,now.AddMinutes(-60),2,options));
    }
}
