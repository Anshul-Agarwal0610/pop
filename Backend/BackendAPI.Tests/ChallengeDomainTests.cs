using BackendAPI.Models;
using Xunit;

namespace BackendAPI.Tests;

public class ChallengeDomainTests
{
    [Fact]
    public void DailyWindowUsesHalfOpenUtcDays()
    {
        var (start, end) = ChallengeDomain.Window(ChallengeRecurrences.Daily, new DateTime(2026, 8, 6, 23, 59, 59, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(start.AddDays(1), end);
    }

    [Theory]
    [InlineData(2026, 8, 3)]
    [InlineData(2026, 8, 9)]
    public void WeeklyWindowRunsMondayToMonday(int year, int month, int day)
    {
        var (start, end) = ChallengeDomain.Window(ChallengeRecurrences.Weekly, new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(DayOfWeek.Monday, start.DayOfWeek);
        Assert.Equal(TimeSpan.FromDays(7), end - start);
    }

    [Theory]
    [InlineData(true, 0, false, "Completed")]
    [InlineData(false, 0, false, "Available")]
    [InlineData(false, 1, false, "InProgress")]
    [InlineData(false, 0, true, "Expired")]
    public void DerivesState(bool completed, int progress, bool expired, string expected)
    {
        var now = DateTime.UtcNow;
        Assert.Equal(expected, ChallengeDomain.State(completed, progress, expired ? now : now.AddSeconds(1), now));
    }

    [Fact]
    public void EligibilityIsCaseInsensitiveAndPrivateAndWellnessAreDeniedByDefault()
    {
        var now = DateTime.UtcNow;
        var challenge = ActiveChallenge(now);
        var poll = PublishedPoll();
        Assert.True(ChallengeDomain.IsEligible(challenge, poll, now));
        poll.IsPrivate = true;
        Assert.False(ChallengeDomain.IsEligible(challenge, poll, now));
        challenge.AllowPrivateVotes = true;
        Assert.True(ChallengeDomain.IsEligible(challenge, poll, now));
        poll.IsWellness = true;
        Assert.False(ChallengeDomain.IsEligible(challenge, poll, now));
        challenge.AllowWellnessVotes = true;
        Assert.True(ChallengeDomain.IsEligible(challenge, poll, now));
    }

    [Fact]
    public void EndBoundaryIsNotEligible()
    {
        var now = DateTime.UtcNow;
        var challenge = ActiveChallenge(now); challenge.EndAt = now;
        Assert.False(ChallengeDomain.IsEligible(challenge, PublishedPoll(), now));
    }

    private static Challenge ActiveChallenge(DateTime now) => new() { IsActive = true, StartAt = now.AddHours(-1), EndAt = now.AddHours(1), RequirementType = "VoteCount", Category = "technology" };
    private static Poll PublishedPoll() => new() { IsActive = true, Category = "Technology", PollMode = PollModes.Public, ModerationStatus = PollModerationStatus.Published };
}
