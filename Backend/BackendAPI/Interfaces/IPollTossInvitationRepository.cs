using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IPollTossInvitationRepository
{
    Task CreateAsync(PollTossInvitation invitation);
    Task<PollTossInvitation?> ConsumeAsync(byte[] tokenHash, DateTime now);
    Task<bool> RevokeAsync(Guid id, long creatorUserId, DateTime now);
    Task<int> PurgeExpiredAsync(DateTime cutoff);
}
