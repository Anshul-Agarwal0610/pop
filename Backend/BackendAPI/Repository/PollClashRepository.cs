using System.Data;
using System.Security.Cryptography;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BackendAPI.Repository;

public sealed class PollClashRepository(DapperContext context, IRewardService rewards) : IPollClashRepository
{
    public async Task<PollClashDto> CreateAsync(long userId, CreatePollClashRequest request, DateTime now)
    {
        if (!PollClashRules.IsValidRoundCount(request.RoundCount)) throw Error("invalid_round_count","Round count must be 1, 3, or 5.");
        if (request.Source is not (PollClashSources.Poll or PollClashSources.GeneratedPack)) throw Error("invalid_source","Source must be Poll or GeneratedPack.");
        if (request.Source == PollClashSources.Poll && request.SeedPollId is null) throw Error("seed_required","A seed poll is required.");
        using var connection=context.CreateConnection(); connection.Open(); using var tx=connection.BeginTransaction(IsolationLevel.Serializable);
        var pollIds=await SelectPolls(connection,tx,request,request.RoundCount,now,null);
        if (pollIds.Count != request.RoundCount) throw Error("insufficient_content","There are not enough eligible polls.");
        var invite=CreateInviteCode();
        var id=await connection.ExecuteScalarAsync<long>(@"INSERT PollClashes(CreatorUserId,InviteCode,Status,Source,RoundCount,CreatedAt,ExpiresAt)
            VALUES(@userId,@invite,'Lobby',@Source,@RoundCount,@now,DATEADD(minute,30,@now)); SELECT CAST(SCOPE_IDENTITY() AS bigint);",new{userId,invite,request.Source,request.RoundCount,now},tx);
        await connection.ExecuteAsync("UPDATE PollClashes SET RootClashId=Id WHERE Id=@id; INSERT PollClashPlayers(ClashId,UserId,Position,JoinedAt) VALUES(@id,@userId,0,@now)",new{id,userId,now},tx);
        for(var i=0;i<pollIds.Count;i++) await connection.ExecuteAsync("INSERT PollClashRounds(ClashId,PollId,Position,Status) VALUES(@id,@pollId,@i,CASE WHEN @i=0 THEN 'Active' ELSE 'Pending' END)",new{id,pollId=pollIds[i],i},tx);
        tx.Commit(); return (await GetAsync(id,userId,now))!;
    }

    public async Task<PollClashDto?> GetAsync(long clashId,long userId,DateTime now)
    { using var c=context.CreateConnection(); return await Load(c,null,clashId,userId,now); }
    public async Task<PollClashDto?> GetInviteAsync(string inviteCode,long userId,DateTime now)
    { using var c=context.CreateConnection(); var id=await c.QuerySingleOrDefaultAsync<long?>("SELECT Id FROM PollClashes WHERE InviteCode=@inviteCode",new{inviteCode}); return id is null?null:await Load(c,null,id.Value,userId,now,true); }
    public async Task<bool> IsParticipantAsync(long clashId,long userId)
    { using var c=context.CreateConnection(); return await c.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM PollClashPlayers WHERE ClashId=@clashId AND UserId=@userId",new{clashId,userId})==1; }

    public async Task<PollClashDto> JoinAsync(long clashId,long userId,DateTime now)
    {
        using var c=context.CreateConnection(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var clash=await c.QuerySingleOrDefaultAsync<ClashRow>("SELECT * FROM PollClashes WITH(UPDLOCK,HOLDLOCK) WHERE Id=@clashId",new{clashId},tx) ?? throw Error("not_found","Clash not found.");
        if(clash.CreatorUserId==userId) throw Error("self_join","The creator cannot join their own Clash.");
        if(clash.Status!="Lobby" || clash.ExpiresAt<=now) throw Error("invite_unavailable","This invite is no longer available.");
        if(await c.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM PollClashPlayers WHERE ClashId=@clashId",new{clashId},tx)>=2) throw Error("clash_full","This Clash already has two players.");
        if(await c.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM UserBlocks WHERE (BlockerUserId=@userId AND BlockedUserId=@creator) OR (BlockerUserId=@creator AND BlockedUserId=@userId)",new{userId,creator=clash.CreatorUserId},tx)>0) throw Error("blocked","This Clash is unavailable.");
        await c.ExecuteAsync("INSERT PollClashPlayers(ClashId,UserId,Position,JoinedAt) VALUES(@clashId,@userId,1,@now); UPDATE PollClashes SET Status='Active',StartedAt=@now WHERE Id=@clashId",new{clashId,userId,now},tx); tx.Commit();
        return (await GetAsync(clashId,userId,now))!;
    }

    public async Task<PollClashDto> RespondAsync(long clashId,long userId,PollClashResponseRequest request,DateTime now)
    {
        bool completed=false; List<(long UserId,long RoundId,bool Correct)> awards=[];
        using(var c=context.CreateConnection()) { c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
            var clash=await c.QuerySingleOrDefaultAsync<ClashRow>("SELECT * FROM PollClashes WITH(UPDLOCK,HOLDLOCK) WHERE Id=@clashId",new{clashId},tx)??throw Error("not_found","Clash not found.");
            if(clash.Status!="Active") throw Error("not_active","Clash is not active.");
            if(await c.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM PollClashPlayers WHERE ClashId=@clashId AND UserId=@userId",new{clashId,userId},tx)!=1) throw new UnauthorizedAccessException();
            var round=await c.QuerySingleOrDefaultAsync<RoundRow>("SELECT * FROM PollClashRounds WITH(UPDLOCK,HOLDLOCK) WHERE Id=@RoundId AND ClashId=@clashId",new{request.RoundId,clashId},tx)??throw Error("round_not_found","Round not found.");
            if(round.Status!="Active") throw Error("round_unavailable","Round is not accepting responses.");
            var valid=await c.QueryAsync<long>("SELECT Id FROM PollOptions WHERE PollId=@PollId",new{round.PollId},tx); var options=valid.ToArray();
            if(!options.Contains(request.OpinionOptionId) || request.PredictedMajorityOptionId is long prediction && !options.Contains(prediction)) throw Error("invalid_option","The selected option does not belong to this poll.");
            try { await c.ExecuteAsync("INSERT PollClashResponses(RoundId,UserId,OpinionOptionId,PredictedMajorityOptionId,SubmittedAt) VALUES(@RoundId,@userId,@OpinionOptionId,@PredictedMajorityOptionId,@now)",new{request.RoundId,userId,request.OpinionOptionId,request.PredictedMajorityOptionId,now},tx); }
            catch(SqlException ex) when(ex.Number is 2601 or 2627) { throw Error("already_submitted","A response was already submitted."); }
            var responses=(await c.QueryAsync<ResponseRow>("SELECT * FROM PollClashResponses WHERE RoundId=@RoundId ORDER BY UserId",new{request.RoundId},tx)).ToList();
            if(responses.Count==2) {
                var counts=(await c.QueryAsync<(long Id,int VoteCount)>("SELECT Id,VoteCount FROM PollOptions WITH(HOLDLOCK) WHERE PollId=@PollId ORDER BY Id",new{round.PollId},tx)).ToArray();
                var majority=PollClashRules.ResolveMajority(counts[0].Id,counts[0].VoteCount,counts[1].Id,counts[1].VoteCount);
                foreach(var response in responses) { var point=PollClashRules.PredictionPoint(response.PredictedMajorityOptionId,majority); await c.ExecuteAsync("UPDATE PollClashResponses SET PredictionPoint=@point WHERE Id=@Id",new{point,response.Id},tx); awards.Add((response.UserId,round.Id,point==1)); }
                await c.ExecuteAsync("UPDATE PollClashRounds SET Status='Revealed',FirstOptionVotes=@a,SecondOptionVotes=@b,ResolvedMajorityOptionId=@majority,RevealedAt=@now WHERE Id=@id",new{a=counts[0].VoteCount,b=counts[1].VoteCount,majority,now,id=round.Id},tx);
                var revealed=await c.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM PollClashRounds WHERE ClashId=@clashId AND Status='Revealed'",new{clashId},tx);
                completed=PollClashRules.CanComplete(revealed,clash.RoundCount);
                if(completed) await c.ExecuteAsync("UPDATE PollClashes SET Status='Completed',CompletedAt=@now,CurrentPosition=RoundCount WHERE Id=@clashId",new{clashId,now},tx);
                else await c.ExecuteAsync("UPDATE PollClashes SET CurrentPosition=CurrentPosition+1 WHERE Id=@clashId; UPDATE PollClashRounds SET Status='Active' WHERE ClashId=@clashId AND Position=@next",new{clashId,next=round.Position+1},tx);
            }
            tx.Commit();
        }
        if(completed) await GrantCompletionRewards(clashId,awards,now);
        return (await GetAsync(clashId,userId,now))!;
    }

    public async Task<PollClashDto> RequestRematchAsync(long clashId,long userId,DateTime now)
    { using var c=context.CreateConnection(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable); var row=await LockParticipant(c,tx,clashId,userId); if(!PollClashRules.CanRematch(row.Status))throw Error("not_completed","Only a completed Clash can be rematched."); try{await c.ExecuteAsync("INSERT PollClashRematches(ClashId,RequestedByUserId,RequestedAt) VALUES(@clashId,@userId,@now)",new{clashId,userId,now},tx);}catch(SqlException ex)when(ex.Number is 2601 or 2627){throw Error("rematch_pending","A rematch request is already pending.");} tx.Commit(); return(await GetAsync(clashId,userId,now))!; }
    public Task<PollClashDto> DeclineRematchAsync(long clashId,long requestId,long userId,DateTime now)=>ResolveRematch(clashId,requestId,userId,now,false);
    public Task<PollClashDto> AcceptRematchAsync(long clashId,long requestId,long userId,DateTime now)=>ResolveRematch(clashId,requestId,userId,now,true);

    private async Task<PollClashDto> ResolveRematch(long clashId,long requestId,long userId,DateTime now,bool accept)
    { using var c=context.CreateConnection(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable); var clash=await LockParticipant(c,tx,clashId,userId); var rematch=await c.QuerySingleOrDefaultAsync<RematchRow>("SELECT * FROM PollClashRematches WITH(UPDLOCK,HOLDLOCK) WHERE Id=@requestId AND ClashId=@clashId",new{requestId,clashId},tx)??throw Error("rematch_not_found","Rematch request not found."); if(rematch.Status!="Pending")throw Error("rematch_resolved","This rematch request was already resolved."); if(rematch.RequestedByUserId==userId)throw Error("requestor_cannot_accept","The other player must respond."); long? successor=null;
      if(accept){var request=new CreatePollClashRequest(null,clash.Source,clash.RoundCount); var excluded=(await c.QueryAsync<long>("SELECT DISTINCT r.PollId FROM PollClashRounds r JOIN PollClashes pc ON pc.Id=r.ClashId WHERE pc.RootClashId=@root",new{root=clash.RootClashId??clash.Id},tx)).ToArray(); var polls=await SelectPolls(c,tx,request,clash.RoundCount,now,excluded); if(polls.Count!=clash.RoundCount)throw Error("insufficient_content","There are not enough fresh polls for a rematch."); var invite=CreateInviteCode(); successor=await c.ExecuteScalarAsync<long>("INSERT PollClashes(CreatorUserId,InviteCode,Status,Source,RoundCount,RootClashId,PreviousClashId,CreatedAt,StartedAt,ExpiresAt) VALUES(@userId,@invite,'Active',@Source,@RoundCount,@root,@clashId,@now,@now,DATEADD(minute,30,@now)); SELECT CAST(SCOPE_IDENTITY() AS bigint)",new{userId,invite,clash.Source,clash.RoundCount,root=clash.RootClashId??clash.Id,clashId,now},tx); var players=await c.QueryAsync<long>("SELECT UserId FROM PollClashPlayers WHERE ClashId=@clashId ORDER BY Position",new{clashId},tx); var pos=0;foreach(var player in players)await c.ExecuteAsync("INSERT PollClashPlayers VALUES(@successor,@player,@pos,@now)",new{successor,player,pos=pos++,now},tx);for(var i=0;i<polls.Count;i++)await c.ExecuteAsync("INSERT PollClashRounds(ClashId,PollId,Position,Status) VALUES(@successor,@poll,@i,CASE WHEN @i=0 THEN 'Active' ELSE 'Pending' END)",new{successor,poll=polls[i],i},tx);}
      await c.ExecuteAsync("UPDATE PollClashRematches SET Status=@status,RespondedAt=@now,ResultingClashId=@successor WHERE Id=@requestId",new{status=accept?"Accepted":"Declined",now,successor,requestId},tx);tx.Commit();return accept?(await GetAsync(successor!.Value,userId,now))!:(await GetAsync(clashId,userId,now))!; }

    private async Task GrantCompletionRewards(long clashId,IEnumerable<(long UserId,long RoundId,bool Correct)> roundAwards,DateTime now)
    { var users=roundAwards.Select(x=>x.UserId).Distinct(); foreach(var user in users){await TryGrant(new(user,RewardRuleCodes.ClashParticipation,"poll-clash-participation",clashId.ToString(),now)); foreach(var round in roundAwards.Where(x=>x.UserId==user&&x.Correct))await TryGrant(new(user,RewardRuleCodes.ClashPrediction,"poll-clash-prediction",$"{clashId}:{round.RoundId}",now));} }
    private async Task TryGrant(RewardGrantRequest request){try{await rewards.GrantAsync(request);}catch(RewardLimitExceededException){/* The match completes; caps only suppress XP. */}}

    private static async Task<ClashRow> LockParticipant(IDbConnection c,IDbTransaction tx,long clashId,long userId){var row=await c.QuerySingleOrDefaultAsync<ClashRow>("SELECT c.* FROM PollClashes c WITH(UPDLOCK,HOLDLOCK) JOIN PollClashPlayers p ON p.ClashId=c.Id WHERE c.Id=@clashId AND p.UserId=@userId",new{clashId,userId},tx);return row??throw new UnauthorizedAccessException();}
    private static async Task<List<long>> SelectPolls(IDbConnection c,IDbTransaction tx,CreatePollClashRequest request,int count,DateTime now,IEnumerable<long>? excluded)
    { var blocked=(excluded??[]).ToArray(); var seed=request.SeedPollId; var rows=await c.QueryAsync<long>(@"SELECT TOP (@count) p.Id FROM Polls p WHERE p.IsActive=1 AND p.ExpiresAt>@now AND ISNULL(p.IsPrivate,0)=0 AND ISNULL(p.IsWellness,0)=0 AND ISNULL(p.IsSponsored,0)=0 AND ISNULL(p.ModerationStatus,'Published')='Published' AND (@generated=0 OR p.IsAIGenerated=1) AND (SELECT COUNT(*) FROM PollOptions o WHERE o.PollId=p.Id)=2 AND (p.Id=@seed OR @seed IS NULL OR p.Id<>@seed) AND p.Id NOT IN @blocked ORDER BY CASE WHEN p.Id=@seed THEN 0 ELSE 1 END,NEWID()",new{count,now,generated=request.Source==PollClashSources.GeneratedPack,seed,blocked=blocked.Length==0?new long[]{-1}:blocked},tx); var result=rows.ToList(); if(seed.HasValue&&!result.Contains(seed.Value))throw Error("poll_ineligible","The selected poll is not eligible for Clash."); return result; }
    private async Task<PollClashDto?> Load(IDbConnection c,IDbTransaction? tx,long id,long viewer,DateTime now,bool allowInvitePreview=false)
    { var clash=await c.QuerySingleOrDefaultAsync<ClashRow>("SELECT * FROM PollClashes WHERE Id=@id",new{id},tx);if(clash is null)return null;var isPlayer=await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PollClashPlayers WHERE ClashId=@id AND UserId=@viewer",new{id,viewer},tx)>0;if(!isPlayer&&!allowInvitePreview)throw new UnauthorizedAccessException();if(clash.Status=="Lobby"&&clash.ExpiresAt<=now){await c.ExecuteAsync("UPDATE PollClashes SET Status='Expired' WHERE Id=@id AND Status='Lobby'",new{id},tx);clash.Status="Expired";}
      var players=(await c.QueryAsync<PlayerRow>("SELECT p.*,COALESCE(NULLIF(u.DisplayName,''),u.Username) DisplayName FROM PollClashPlayers p JOIN Users u ON u.Id=p.UserId WHERE p.ClashId=@id ORDER BY p.Position",new{id},tx)).ToList();var rounds=(await c.QueryAsync<RoundViewRow>("SELECT r.*,p.Question FROM PollClashRounds r JOIN Polls p ON p.Id=r.PollId WHERE r.ClashId=@id ORDER BY r.Position",new{id},tx)).ToList();var responses=(await c.QueryAsync<ResponseRow>("SELECT x.* FROM PollClashResponses x JOIN PollClashRounds r ON r.Id=x.RoundId WHERE r.ClashId=@id",new{id},tx)).ToList();var playerDtos=players.Select(p=>{var score=responses.Where(x=>x.UserId==p.UserId).Sum(x=>x.PredictionPoint);var current=responses.FirstOrDefault(x=>rounds.Any(r=>r.Id==x.RoundId&&r.Status=="Active")&&x.UserId==p.UserId);return new PollClashPlayerDto(p.UserId,p.DisplayName,p.UserId==viewer,current is not null,current?.OpinionOptionId,current?.PredictedMajorityOptionId,score);}).ToList();var roundDtos=new List<PollClashRoundDto>();int agreement=0;foreach(var r in rounds){var opts=(await c.QueryAsync<PollClashOptionDto>("SELECT Id,Text,CASE WHEN @revealed=1 THEN VoteCount ELSE NULL END PublicVotes FROM PollOptions WHERE PollId=@PollId ORDER BY Id",new{revealed=r.Status=="Revealed",r.PollId},tx)).ToList();var rr=responses.Where(x=>x.RoundId==r.Id).ToList();bool? agreed=r.Status=="Revealed"&&rr.Count==2?rr[0].OpinionOptionId==rr[1].OpinionOptionId:null;if(agreed==true)agreement++;var revealed=r.Status=="Revealed"?rr.Select(x=>new PollClashRevealedOpinionDto(x.UserId,players.First(p=>p.UserId==x.UserId).DisplayName,x.OpinionOptionId,x.PredictedMajorityOptionId,x.PredictionPoint)).ToList():[];roundDtos.Add(new(r.Id,r.Position,r.PollId,r.Question,r.Status,opts,r.ResolvedMajorityOptionId,agreed,rr.FirstOrDefault(x=>x.UserId==viewer)?.PredictionPoint??0,revealed));}var completed=rounds.Count(x=>x.Status=="Revealed");var winner=clash.Status=="Completed"&&players.Count==2&&playerDtos[0].PredictionScore!=playerDtos[1].PredictionScore?(playerDtos[0].PredictionScore>playerDtos[1].PredictionScore?players[0].UserId:players[1].UserId):(long?)null;var rematch=await c.QuerySingleOrDefaultAsync<PollClashRematchDto>("SELECT TOP 1 Id,RequestedByUserId,Status,ResultingClashId FROM PollClashRematches WHERE ClashId=@id ORDER BY Id DESC",new{id},tx);var awarded=await c.ExecuteScalarAsync<int>("SELECT COALESCE(SUM(Value),0) FROM RewardEvents WHERE UserId=@viewer AND SourceReference LIKE @prefix AND RuleCode IN ('clash.participation','clash.prediction')",new{viewer,prefix=$"{id}%"},tx);return new(clash.Id,clash.InviteCode,clash.Status,clash.Source,clash.RoundCount,completed,clash.ExpiresAt,playerDtos,roundDtos,agreement,winner,new(awarded,false,false),rematch); }
    private static string CreateInviteCode()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(6)); private static PollClashException Error(string code,string message)=>new(code,message);
    private sealed class ClashRow{public long Id{get;set;}public long CreatorUserId{get;set;}public string InviteCode{get;set;}="";public string Status{get;set;}="";public string Source{get;set;}="";public int RoundCount{get;set;}public long? RootClashId{get;set;}public DateTime ExpiresAt{get;set;}}
    private class RoundRow{public long Id{get;set;}public long PollId{get;set;}public int Position{get;set;}public string Status{get;set;}="";}
    private sealed class RoundViewRow:RoundRow{public string Question{get;set;}="";public long? ResolvedMajorityOptionId{get;set;}}
    private sealed class PlayerRow{public long UserId{get;set;}public string DisplayName{get;set;}="";}
    private sealed class ResponseRow{public long Id{get;set;}public long RoundId{get;set;}public long UserId{get;set;}public long OpinionOptionId{get;set;}public long? PredictedMajorityOptionId{get;set;}public int PredictionPoint{get;set;}}
    private sealed class RematchRow{public long Id{get;set;}public long RequestedByUserId{get;set;}public string Status{get;set;}="";}
}
