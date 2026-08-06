using System.Security.Cryptography;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Microsoft.Extensions.Options;

namespace BackendAPI.Services;

public sealed class PollTossService(IPollTossInvitationRepository invitations, IPollsRepository polls, ISystemClock clock, IOptions<NearbyPollTossOptions> options)
{
    public async Task<(PollTossInvitation Invitation, string Token)?> CreateAsync(long pollId, long userId)
    {
        var poll = await polls.GetByIdAsync(pollId, userId);
        if (!PollTossRules.IsEligible(poll, clock.UtcNow)) return null;
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var invitation = new PollTossInvitation { Id=Guid.NewGuid(), TokenHash=Hash(token), PollId=pollId, CreatorUserId=userId, CreatedAt=clock.UtcNow, ExpiresAt=clock.UtcNow.AddSeconds(Math.Clamp(options.Value.InvitationTtlSeconds, 30, 300)) };
        await invitations.CreateAsync(invitation);
        return (invitation, token);
    }

    public async Task<Poll?> RedeemAsync(string token, long userId)
    {
        if (!IsTokenWellFormed(token)) return null;
        var invitation = await invitations.ConsumeAsync(Hash(token), clock.UtcNow);
        if (invitation is null) return null;
        var poll = await polls.GetByIdAsync(invitation.PollId, userId);
        return PollTossRules.IsEligible(poll, clock.UtcNow) ? poll : null;
    }

    public static bool IsTokenWellFormed(string? token) => token is { Length: 43 } && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    public static byte[] Hash(string token) => SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
}
