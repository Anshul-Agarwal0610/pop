using BackendAPI.Models;
namespace BackendAPI.Interfaces;
public interface IPollPacksRepository
{
 Task<IReadOnlyList<PollPackDto>> PublishedAsync(); Task<IReadOnlyList<PollPackDto>> MineAsync(long ownerId);
 Task<PollPackDto> CreateAsync(long ownerId,SavePollPackRequest request); Task<PollPackDto> UpdateAsync(long id,long ownerId,SavePollPackRequest request);
 Task<PollPackDto> SubmitAsync(long id,long ownerId); Task<PollPackDto> ModerateAsync(long id,long moderatorId,ModeratePollPackRequest request);
 PollPackDto GetUsable(long id,long ownerId);
}
