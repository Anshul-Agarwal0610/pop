using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class GamificationRulesTests
{
    private static readonly DateTime Today = new(2026, 8, 6, 0, 1, 0, DateTimeKind.Utc);

    [Fact]
    public void FirstVoteStartsAtOne()
    {
        var result = GamificationRules.ApplyDailyStreak(0, 0, null, null, Today);
        Assert.Equal(1, result.Streak);
        Assert.True(result.StreakAdvanced);
        Assert.True(result.TodayComplete);
    }

    [Fact]
    public void ConsecutiveUtcDaysAdvanceOnce()
    {
        var result = GamificationRules.ApplyDailyStreak(2, 2, Today.AddDays(-1), null, Today);
        Assert.Equal(3, result.Streak);
        Assert.Equal(3, result.MilestoneReached);
    }

    [Fact]
    public void SameUtcDayDoesNotAdvanceOrRepeatMilestone()
    {
        var result = GamificationRules.ApplyDailyStreak(3, 3, Today.AddMinutes(-1), null, Today);
        Assert.Equal(3, result.Streak);
        Assert.False(result.StreakAdvanced);
        Assert.Null(result.MilestoneReached);
    }

    [Fact]
    public void OppositeSidesOfUtcMidnightAreDifferentDays()
    {
        var beforeMidnight = new DateTime(2026, 8, 5, 23, 59, 59, DateTimeKind.Utc);
        var result = GamificationRules.ApplyDailyStreak(4, 6, beforeMidnight, null, Today);
        Assert.Equal(5, result.Streak);
        Assert.Equal(6, result.LongestStreak);
    }

    [Fact]
    public void GapWithoutRecoveryResetsButPreservesLongest()
    {
        var result = GamificationRules.ApplyDailyStreak(6, 9, Today.AddDays(-2), null, Today);
        Assert.Equal(1, result.Streak);
        Assert.Equal(9, result.LongestStreak);
        Assert.True(result.RecoveryEligible);
        Assert.False(result.RecoveryUsed);
    }

    [Fact]
    public void EligibleRecoveryContinuesStreakAndIsConsumed()
    {
        var result = GamificationRules.ApplyDailyStreak(6, 6, Today.AddDays(-2), Today.AddDays(-31), Today, true);
        Assert.Equal(7, result.Streak);
        Assert.True(result.RecoveryUsed);
        Assert.Equal(7, result.MilestoneReached);
    }

    [Fact]
    public void RecoveryInsideCooldownCannotBeConsumed()
    {
        var result = GamificationRules.ApplyDailyStreak(6, 6, Today.AddDays(-2), Today.AddDays(-10), Today, true);
        Assert.Equal(1, result.Streak);
        Assert.False(result.RecoveryEligible);
        Assert.False(result.RecoveryUsed);
    }

    [Fact]
    public void LongerGapIsNotRecoverable()
    {
        var result = GamificationRules.ApplyDailyStreak(10, 10, Today.AddDays(-3), null, Today, true);
        Assert.Equal(1, result.Streak);
        Assert.False(result.RecoveryEligible);
    }
}
