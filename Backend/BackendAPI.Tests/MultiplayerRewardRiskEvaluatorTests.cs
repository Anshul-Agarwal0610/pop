using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class MultiplayerRewardRiskEvaluatorTests
{
    private static MultiplayerRiskContext Context(bool self=false,bool replay=false,bool cycling=false,bool device=false,bool network=false,bool timing=false) => new(Guid.NewGuid(),Guid.NewGuid(),"completion",self,replay,cycling,device,network,timing,false);
    [Fact] public void Self_invite_suppresses_only_reward() { var d=new MultiplayerRewardRiskEvaluator().Evaluate(Context(self:true)); Assert.Equal(RewardRiskOutcome.Suppress,d.Outcome); Assert.False(d.IsPermanentBan); }
    [Fact] public void Replay_suppresses_and_has_stable_policy() { var d=new MultiplayerRewardRiskEvaluator().Evaluate(Context(replay:true)); Assert.Equal(RewardRiskOutcome.Suppress,d.Outcome); Assert.Equal(MultiplayerRewardRiskEvaluator.PolicyVersion,d.PolicyVersion); }
    [Theory,InlineData(true,false),InlineData(false,true)] public void One_weak_match_never_suppresses(bool device,bool network) => Assert.NotEqual(RewardRiskOutcome.Suppress,new MultiplayerRewardRiskEvaluator().Evaluate(Context(device:device,network:network)).Outcome);
    [Fact] public void Multiple_independent_signals_hold() => Assert.Equal(RewardRiskOutcome.Hold,new MultiplayerRewardRiskEvaluator().Evaluate(Context(cycling:true,device:true,network:true)).Outcome);
    [Fact] public void Four_independent_signals_suppress_without_ban() { var d=new MultiplayerRewardRiskEvaluator().Evaluate(Context(cycling:true,device:true,network:true,timing:true)); Assert.Equal(RewardRiskOutcome.Suppress,d.Outcome); Assert.False(d.IsPermanentBan); }
}
