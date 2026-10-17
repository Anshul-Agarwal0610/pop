using BackendAPI.Models;
using Xunit;

namespace BackendAPI.Tests;

public class PollResultVisibilityTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(PollResultVisibility.Immediate, false, false, true)]
    [InlineData(PollResultVisibility.AfterVote, false, false, false)]
    [InlineData(PollResultVisibility.AfterVote, true, false, true)]
    [InlineData(PollResultVisibility.AfterClose, true, false, false)]
    [InlineData(PollResultVisibility.AfterClose, false, true, true)]
    public void RevealPolicyHidesOrShowsCounts(string mode, bool voted, bool closed, bool expected)
    {
        var poll = new Poll
        {
            ResultVisibility = mode,
            HasVoted = voted,
            CreatedByUserId = 9,
            ExpiresAt = closed ? Now.AddSeconds(-1) : Now.AddDays(1),
            TotalVotes = 12,
            Options = new List<PollOption> { new() { VoteCount = 7, VotePercentage = 58.3 } }
        };

        PollResultVisibility.Apply(poll, 2, Now);

        Assert.Equal(expected, poll.CanViewResults);
        Assert.Equal(expected ? 12 : 0, poll.TotalVotes);
        Assert.Equal(expected ? 7 : 0, poll.Options[0].VoteCount);
        Assert.Equal(expected ? 58.3 : 0, poll.Options[0].VotePercentage);
    }

    [Fact]
    public void CreatorCanAlwaysSeeResults()
    {
        var poll = new Poll { ResultVisibility = PollResultVisibility.AfterClose, CreatedByUserId = 9, ExpiresAt = Now.AddDays(1) };
        PollResultVisibility.Apply(poll, 9, Now);
        Assert.True(poll.CanViewResults);
    }
}
