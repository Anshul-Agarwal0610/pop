using BackendAPI.Models; using BackendAPI.Services; using Xunit;
namespace BackendAPI.Tests;
public sealed class LiveRoomRulesTests
{
 [Theory][InlineData(LiveRoomStatus.Lobby,"start")][InlineData(LiveRoomStatus.Active,"pause")][InlineData(LiveRoomStatus.Paused,"resume")][InlineData(LiveRoomStatus.Active,"close")][InlineData(LiveRoomStatus.Active,"advance")]
 public void Legal_transitions_are_accepted(LiveRoomStatus status,string command)=>LiveRoomRules.EnsureTransition(status,command);
 [Theory][InlineData(LiveRoomStatus.Lobby,"pause")][InlineData(LiveRoomStatus.Paused,"start")][InlineData(LiveRoomStatus.Ended,"advance")]
 public void Illegal_transitions_are_rejected(LiveRoomStatus status,string command)=>Assert.Throws<LiveRoomException>(()=>LiveRoomRules.EnsureTransition(status,command));
 [Fact] public void Participant_limit_is_an_exact_boundary(){LiveRoomRules.EnsureCanJoin(49,50,LiveRoomStatus.Lobby,DateTime.UtcNow.AddMinutes(1),DateTime.UtcNow);Assert.Throws<LiveRoomException>(()=>LiveRoomRules.EnsureCanJoin(50,50,LiveRoomStatus.Lobby,DateTime.UtcNow.AddMinutes(1),DateTime.UtcNow));}
 [Fact] public void Late_joiner_is_eligible_only_from_next_round(){Assert.False(LiveRoomRules.IsEligible(3,2));Assert.True(LiveRoomRules.IsEligible(3,3));}
 [Fact] public void Expired_room_rejects_join()=>Assert.Throws<LiveRoomException>(()=>LiveRoomRules.EnsureCanJoin(0,2,LiveRoomStatus.Lobby,DateTime.UtcNow.AddSeconds(-1),DateTime.UtcNow));
 [Fact] public void Predict_majority_scores_predictions_and_ties_score_zero(){var a=Guid.NewGuid();var b=Guid.NewGuid();var c=Guid.NewGuid();var votes=new Dictionary<Guid,(BinaryChoice,BinaryChoice?)>{{a,(BinaryChoice.Up,BinaryChoice.Up)},{b,(BinaryChoice.Up,BinaryChoice.Against)},{c,(BinaryChoice.Against,BinaryChoice.Up)}};var score=LiveRoomScoring.Score(LiveRoomMode.PredictMajority,new(),votes);Assert.Equal(1,score[a]);Assert.Equal(0,score[b]);var tie=new Dictionary<Guid,(BinaryChoice,BinaryChoice?)>{{a,votes[a]},{c,votes[c]}};Assert.All(LiveRoomScoring.Score(LiveRoomMode.PredictMajority,new(),tie).Values,x=>Assert.Equal(0,x));}
 [Fact] public void Consensus_uses_configured_threshold(){var votes=Enumerable.Range(0,4).ToDictionary(_=>Guid.NewGuid(),i=>(i<3?BinaryChoice.Up:BinaryChoice.Against,(BinaryChoice?)null));Assert.All(LiveRoomScoring.Score(LiveRoomMode.ConsensusChallenge,new(.75),votes).Values,x=>Assert.Equal(1,x));Assert.All(LiveRoomScoring.Score(LiveRoomMode.ConsensusChallenge,new(.8),votes).Values,x=>Assert.Equal(0,x));}
}
