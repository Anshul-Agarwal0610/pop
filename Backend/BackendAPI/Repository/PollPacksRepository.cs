using BackendAPI.Interfaces; using BackendAPI.Models;
namespace BackendAPI.Repository;
public sealed class PollPacksRepository : IPollPacksRepository
{
 readonly object gate=new(); readonly List<PollPackDto> packs=[]; long next=1;
 public PollPacksRepository(){ packs.Add(Build(next++,0,new("Party Icebreakers","Starter pack",PollPackVisibility.Public,
     ["Pineapple belongs on pizza","Working from home improves teamwork","A hot dog is a sandwich"]),PollPackModerationStatus.Published)); }
 public Task<IReadOnlyList<PollPackDto>> PublishedAsync()=>Task.FromResult<IReadOnlyList<PollPackDto>>(packs.Where(x=>x.ModerationStatus==PollPackModerationStatus.Published&&x.Visibility==PollPackVisibility.Public).ToList());
 public Task<IReadOnlyList<PollPackDto>> MineAsync(long ownerId)=>Task.FromResult<IReadOnlyList<PollPackDto>>(packs.Where(x=>x.OwnerId==ownerId).ToList());
 public Task<PollPackDto> CreateAsync(long ownerId,SavePollPackRequest r){lock(gate){var p=Build(next++,ownerId,r,PollPackModerationStatus.Draft);packs.Add(p);return Task.FromResult(p);}}
 public Task<PollPackDto> UpdateAsync(long id,long ownerId,SavePollPackRequest r){lock(gate){var i=Index(id,ownerId);if(packs[i].ModerationStatus!=PollPackModerationStatus.Draft)throw new PollPackException("frozen","Only drafts can be edited.");packs[i]=Build(id,ownerId,r,PollPackModerationStatus.Draft);return Task.FromResult(packs[i]);}}
 public Task<PollPackDto> SubmitAsync(long id,long ownerId){lock(gate){var i=Index(id,ownerId);var p=packs[i];if(p.ModerationStatus!=PollPackModerationStatus.Draft)throw new PollPackException("invalid_state","Only drafts can be submitted.");packs[i]=p with{ModerationStatus=PollPackModerationStatus.PendingReview};return Task.FromResult(packs[i]);}}
 public Task<PollPackDto> ModerateAsync(long id,long moderatorId,ModeratePollPackRequest r){lock(gate){var i=packs.FindIndex(x=>x.Id==id);if(i<0)throw new PollPackException("not_found","Pack not found.");if(r.Status is not(PollPackModerationStatus.Published or PollPackModerationStatus.Rejected))throw new PollPackException("invalid_state","Moderators may publish or reject.");packs[i]=packs[i] with{ModerationStatus=r.Status};return Task.FromResult(packs[i]);}}
 public PollPackDto GetUsable(long id,long ownerId){lock(gate){var p=packs.FirstOrDefault(x=>x.Id==id)??throw new PollPackException("not_found","Pack not found.");if(p.OwnerId!=ownerId&&p.ModerationStatus!=PollPackModerationStatus.Published)throw new UnauthorizedAccessException();return p;}}
 int Index(long id,long owner){var i=packs.FindIndex(x=>x.Id==id);if(i<0)throw new PollPackException("not_found","Pack not found.");if(packs[i].OwnerId!=owner)throw new UnauthorizedAccessException();return i;}
 static PollPackDto Build(long id,long owner,SavePollPackRequest r,PollPackModerationStatus state){if(string.IsNullOrWhiteSpace(r.Name)||r.Propositions.Count==0||r.Propositions.Any(string.IsNullOrWhiteSpace))throw new PollPackException("invalid_proposition","A pack needs nonblank propositions.");var items=r.Propositions.Select((x,i)=>new PollPackItemDto(i+1,i,x.Trim(),["Up","Against"])).ToList();return new(id,owner,r.Name.Trim(),r.Description.Trim(),r.Visibility,state,items);}
}
