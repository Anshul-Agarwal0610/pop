using BackendAPI.Models;
namespace BackendAPI.Interfaces;
public interface IPollTossRepository {
 Task<(PollTossInvitation Invitation,string Token)> CreateAsync(long pollId,long senderId,DateTime now);
 Task<PollTossInvitation?> GetForSenderAsync(Guid id,long senderId,DateTime now);
 Task<PollTossInvitation?> PreviewByTokenAsync(string token,DateTime now);
 Task<PollTossInvitation?> PreviewByRoomCodeAsync(string code,DateTime now);
 Task<PollTossInvitation> AcceptAsync(string token,long recipientId,DateTime now);
 Task<PollTossInvitation> CancelAsync(Guid id,long senderId,DateTime now);
}
