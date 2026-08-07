using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class LiveSessionRulesTests
{
    private static readonly DateTime Now=new(2026,8,6,12,0,0,DateTimeKind.Utc);

    [Theory]
    [InlineData(LiveGameMode.Clash,false,null)]
    [InlineData(LiveGameMode.Room,false,null)]
    [InlineData(LiveGameMode.Relay,true,null)]
    [InlineData(LiveGameMode.Bomb,false,3)]
    public void Supported_mode_configurations_are_valid(LiveGameMode mode,bool teams,int? lives)=>LiveSessionRules.Validate(mode,new(8,60,60,lives,teams));

    [Fact] public void Bomb_requires_lives()=>Assert.Equal("invalid_mode_config",Assert.Throws<LiveSessionException>(()=>LiveSessionRules.Validate(LiveGameMode.Bomb,new())).Code);
    [Fact] public void Relay_requires_teams()=>Assert.Throws<LiveSessionException>(()=>LiveSessionRules.Validate(LiveGameMode.Relay,new()));
    [Theory] [InlineData(1,60,60)] [InlineData(8,9,60)] [InlineData(8,60,241)]
    public void Shared_limits_are_enforced(int participants,int seconds,int minutes)=>Assert.Throws<LiveSessionException>(()=>LiveSessionRules.Validate(LiveGameMode.Room,new(participants,seconds,minutes)));
    [Fact] public void Expiry_is_inclusive(){Assert.False(LiveSessionRules.IsExpired(Now.AddTicks(1),Now));Assert.True(LiveSessionRules.IsExpired(Now,Now));}
    [Fact] public void Abandonment_is_inclusive(){Assert.False(LiveSessionRules.IsAbandoned(Now-LiveSessionRules.AbandonmentThreshold+TimeSpan.FromTicks(1),Now));Assert.True(LiveSessionRules.IsAbandoned(Now-LiveSessionRules.AbandonmentThreshold,Now));}
    [Fact] public void Only_legal_transitions_are_allowed(){Assert.True(LiveSessionRules.CanTransition(LiveSessionStatus.Lobby,LiveSessionStatus.Active));Assert.True(LiveSessionRules.CanTransition(LiveSessionStatus.Active,LiveSessionStatus.Completed));Assert.False(LiveSessionRules.CanTransition(LiveSessionStatus.Completed,LiveSessionStatus.Active));Assert.False(LiveSessionRules.CanTransition(LiveSessionStatus.Expired,LiveSessionStatus.Abandoned));}
    [Fact] public void Reward_keys_are_deterministic(){Assert.Equal("live-session:7:completion:11",LiveSessionRules.CompletionRewardSource(7,11));Assert.Equal("live-session:7:round:9:winner:11",LiveSessionRules.RoundWinnerRewardSource(7,9,11));}
}
