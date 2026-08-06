using BackendAPI.Models;
namespace BackendAPI.Services;
internal static class LiveRoomProjector
{
    public static HostRoomSnapshot Host(LiveRoomState r) => new(r.Id,r.Code,r.Status,r.Mode,r.Version,
        r.Participants.Where(p=>!p.Removed).Select(p=>Dto(r,p)).ToList(),Round(r,true),r.DisplayToken);
    public static ParticipantRoomSnapshot Participant(LiveRoomState r, LiveParticipantState p) =>
        new(r.Id,p.Id,r.Status,r.Mode,r.Version,p.Score,LiveRoomRules.IsEligible(p.EligibleFrom,r.Position),
            r.Round?.Votes.ContainsKey(p.Id)==true,Round(r,r.Round?.Status==LiveRoundStatus.Revealed));
    public static DisplayRoomSnapshot Display(LiveRoomState r) => new(r.Id,r.Code,r.Status,r.Mode,r.Version,
        r.Participants.Count(p=>!p.Removed),r.Participants.Where(p=>!p.Removed).Select(p=>Dto(r,p)).OrderByDescending(p=>p.Score).ToList(),
        Round(r,r.Round?.Status==LiveRoundStatus.Revealed));
    private static ParticipantDto Dto(LiveRoomState r, LiveParticipantState p) => new(p.Id,p.Name,p.Score,true,LiveRoomRules.IsEligible(p.EligibleFrom,r.Position));
    private static LiveRoundDto? Round(LiveRoomState r,bool reveal) { var x=r.Round;if(x is null)return null;
        var eligible=r.Participants.Count(p=>!p.Removed&&LiveRoomRules.IsEligible(p.EligibleFrom,x.Position));
        return new(x.Position,x.Proposition,x.Status,x.Votes.Count,eligible,reveal?x.Votes.Count(v=>v.Value.Choice==BinaryChoice.Up):null,
            reveal?x.Votes.Count(v=>v.Value.Choice==BinaryChoice.Against):null); }
}
