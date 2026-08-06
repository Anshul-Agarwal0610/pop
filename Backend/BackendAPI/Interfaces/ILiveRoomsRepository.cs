using BackendAPI.Models;
namespace BackendAPI.Interfaces;
public interface ILiveRoomsRepository
{
 Task<HostRoomSnapshot> CreateAsync(long hostId, CreateLiveRoomRequest request, DateTime now);
 Task<HostRoomSnapshot> HostAsync(Guid id,long hostId,DateTime now);
 Task<DisplayRoomSnapshot> DisplayAsync(Guid id,string token,DateTime now);
 Task<JoinLiveRoomResponse> JoinAsync(JoinLiveRoomRequest request,DateTime now);
 Task<ParticipantRoomSnapshot> ParticipantAsync(Guid id,string token,DateTime now);
 Task<ParticipantRoomSnapshot> VoteAsync(Guid id,string token,LiveVoteRequest request,DateTime now);
 Task<HostRoomSnapshot> CommandAsync(Guid id,long hostId,string command,Guid? participantId,DateTime now);
 Task ExpireAsync(DateTime now);
}
