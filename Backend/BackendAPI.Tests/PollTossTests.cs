using BackendAPI.Models;
using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public sealed class PollTossTests
{
    private static Poll Eligible(DateTime now) => new() { IsActive=true, IsPrivate=false, IsWellness=false, PollMode=PollModes.Public, ModerationStatus=PollModerationStatus.Published, Category="Technology", ExpiresAt=now.AddMinutes(1) };

    [Fact] public void EligiblePollMustRemainPublicPublishedAndCurrent()
    {
        var now=DateTime.UtcNow; var poll=Eligible(now); Assert.True(PollTossRules.IsEligible(poll,now));
        poll.IsPrivate=true; Assert.False(PollTossRules.IsEligible(poll,now)); poll=Eligible(now); poll.IsWellness=true; Assert.False(PollTossRules.IsEligible(poll,now));
        poll=Eligible(now); poll.Category="Health"; Assert.False(PollTossRules.IsEligible(poll,now)); poll=Eligible(now); poll.ExpiresAt=now; Assert.False(PollTossRules.IsEligible(poll,now));
        poll=Eligible(now); poll.ModerationStatus=PollModerationStatus.Flagged; Assert.False(PollTossRules.IsEligible(poll,now));
    }

    [Theory] [InlineData("")] [InlineData("poll-12")] [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    public void MalformedTokensFailClosed(string token) => Assert.False(PollTossService.IsTokenWellFormed(token));

    [Fact] public void HashIsDeterministicAndDoesNotContainToken()
    {
        var token=new string('A',43); Assert.True(PollTossService.IsTokenWellFormed(token)); var hash=PollTossService.Hash(token); Assert.Equal(32,hash.Length); Assert.Equal(hash,PollTossService.Hash(token)); Assert.NotEqual(System.Text.Encoding.UTF8.GetBytes(token),hash);
    }
}
