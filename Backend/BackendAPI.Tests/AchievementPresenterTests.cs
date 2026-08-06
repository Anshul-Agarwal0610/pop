using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;
public class AchievementPresenterTests
{
    private static AchievementBadge Badge(bool secret=false) => new() { Id=1,Code="test",Name="Explorer",Description="Explore",Icon="Compass",Category="Exploration",RuleType=AchievementRuleType.DistinctCategoriesVoted,Threshold=3,RewardXp=75,RequirementText="Vote in 3 categories",ProgressVisible=true,IsSecret=secret };
    [Fact] public void Earned_includes_date_and_reward() { var date=DateTime.UtcNow; var result=AchievementPresenter.Present(Badge(),new UserBadge{Id=4,AwardedAt=date},new(3,0,0,0,3)); Assert.Equal(AchievementStatus.Earned,result.Status);Assert.Equal(date,result.AwardedAt);Assert.Equal(75,result.RewardXp); }
    [Fact] public void Locked_safe_badge_includes_requirement_and_capped_progress() { var result=AchievementPresenter.Present(Badge(),null,new(0,0,0,0,9)); Assert.Equal("Vote in 3 categories",result.Requirement);Assert.Equal(3,result.CurrentProgress);Assert.Equal(100,result.ProgressPercent); }
    [Fact] public void Secret_badge_redacts_requirement_and_progress() { var result=AchievementPresenter.Present(Badge(true),null,new(0,0,0,0,2)); Assert.Equal("Secret achievement",result.Name);Assert.Null(result.Requirement);Assert.Null(result.CurrentProgress);Assert.DoesNotContain("Explore",result.Description); }
    [Fact] public void Below_threshold_with_activity_is_in_progress() { var result=AchievementPresenter.Present(Badge(),null,new(0,0,0,0,2)); Assert.Equal(AchievementStatus.InProgress,result.Status);Assert.Equal(66,result.ProgressPercent); }
}
