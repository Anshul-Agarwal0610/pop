using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class PollClashRulesTests
{
    [Theory] [InlineData(1,true)] [InlineData(3,true)] [InlineData(5,true)] [InlineData(2,false)] [InlineData(0,false)]
    public void Only_one_three_or_five_rounds_are_valid(int count,bool expected) => Assert.Equal(expected,PollClashRules.IsValidRoundCount(count));
    [Fact] public void Tied_public_result_is_unresolved_and_awards_no_point() { var majority=PollClashRules.ResolveMajority(1,4,2,4); Assert.Null(majority); Assert.Equal(0,PollClashRules.PredictionPoint(1,majority)); }
    [Fact] public void Correct_prediction_scores_but_wrong_and_omitted_do_not() { Assert.Equal(1,PollClashRules.PredictionPoint(2,2)); Assert.Equal(0,PollClashRules.PredictionPoint(1,2)); Assert.Equal(0,PollClashRules.PredictionPoint(null,2)); }
    [Fact] public void Agreement_is_independent_of_prediction_score() { var score=PollClashRules.Score(new[]{(1L,1L,(long?)2,(long?)1,(long?)2)}); Assert.Equal(1,score.AgreementCount); Assert.Equal(1,score.FirstPredictionScore); Assert.Equal(0,score.SecondPredictionScore); Assert.Equal(0,score.WinnerIndex); }
    [Fact] public void Equal_prediction_scores_have_no_winner() { var score=PollClashRules.Score(new[]{(1L,2L,(long?)null,(long?)null,(long?)null)}); Assert.Null(score.WinnerIndex); Assert.Equal(1,score.CompletedRounds); }
    [Fact] public void Incomplete_round_cannot_reveal_or_complete() { Assert.False(PollClashRules.CanReveal(1)); Assert.False(PollClashRules.CanComplete(2,3)); Assert.True(PollClashRules.CanReveal(2)); }
    [Fact] public void Only_completed_clashes_can_be_rematched() { Assert.True(PollClashRules.CanRematch(PollClashStatuses.Completed)); Assert.False(PollClashRules.CanRematch(PollClashStatuses.Active)); }
}
