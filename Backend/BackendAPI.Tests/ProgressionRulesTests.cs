using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class ProgressionRulesTests
{
    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(999, 1, 999, 99)]
    [InlineData(1000, 2, 0, 0)]
    [InlineData(2500, 3, 500, 50)]
    public void FromTotalXp_UsesDocumentedBoundaries(int xp, int level, int intoLevel, int percent)
    {
        var result = GamificationRules.FromTotalXp(xp);
        Assert.Equal(level, result.Level);
        Assert.Equal(intoLevel, result.XpIntoLevel);
        Assert.Equal(percent, result.ProgressPercent);
        Assert.Equal(xp, result.TotalXp);
    }

    [Fact]
    public void FromTotalXp_RejectsNegativeBalances() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => GamificationRules.FromTotalXp(-1));

    [Theory]
    [InlineData(false, 25)]
    [InlineData(true, 35)]
    public void VoteXp_IsCentralized(bool trending, int expected) =>
        Assert.Equal(expected, GamificationRules.VoteXp(new Poll { IsTrending = trending }));

    [Fact]
    public void Reward_ReportsMultiLevelCrossing()
    {
        var reward = new ProgressionReward
        {
            PreviousLevel = 1,
            AwardedXp = 2100,
            Progression = GamificationRules.FromTotalXp(2100)
        };
        Assert.True(reward.LeveledUp);
        Assert.Equal(2, reward.LevelsGained);
        Assert.Equal(3, reward.Level);
    }
}
