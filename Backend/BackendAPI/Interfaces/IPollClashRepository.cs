using BackendAPI.Models;
namespace BackendAPI.Interfaces;
public interface IPollClashRepository
{
 Task<PollClashDto> CreateAsync(long userId, CreatePollClashRequest request, DateTime now);
 Task<PollClashDto?> GetAsync(long clashId, long userId, DateTime now);
 Task<PollClashDto?> GetInviteAsync(string inviteCode, long userId, DateTime now);
 Task<PollClashDto> JoinAsync(long clashId, long userId, DateTime now);
 Task<PollClashDto> RespondAsync(long clashId, long userId, PollClashResponseRequest request, DateTime now);
 Task<PollClashDto> RequestRematchAsync(long clashId, long userId, DateTime now);
 Task<PollClashDto> AcceptRematchAsync(long clashId, long requestId, long userId, DateTime now);
 Task<PollClashDto> DeclineRematchAsync(long clashId, long requestId, long userId, DateTime now);
 Task<bool> IsParticipantAsync(long clashId, long userId);
}
