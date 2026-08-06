using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BackendAPI.Repository;

public sealed class LiveSessionsRepository(DapperContext context, IRewardService rewards) : ILiveSessionsRepository
{
    public async Task<LiveSessionDto> CreateAsync(long hostUserId, CreateLiveSessionRequest request, DateTime utcNow)
    {
        EnsureUtc(utcNow); LiveSessionRules.Validate(request.Mode, request.Configuration);
        using var conn = (SqlConnection)context.CreateConnection(); await conn.OpenAsync();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var polls = await EligiblePolls(conn, tx, hostUserId, request, utcNow);
        if (polls.Count == 0) throw new LiveSessionException("content_ineligible", "The poll or poll pack is not eligible for live play.");
        var code = await UniqueJoinCode(conn, tx);
        var id = await conn.ExecuteScalarAsync<long>(@"
INSERT dbo.LiveSessions(HostUserId,Mode,ModeConfiguration,ContentType,PollId,PollPackId,Status,JoinCode,CreatedAt,LastActivityAt,ExpiresAt)
OUTPUT INSERTED.Id VALUES(@Host,@Mode,@Config,@ContentType,@PollId,@PackId,'Lobby',@Code,@Now,@Now,@Expires)", new {
            Host=hostUserId, Mode=request.Mode.ToString(), Config=JsonSerializer.Serialize(request.Configuration),
            ContentType=request.ContentType.ToString(), PollId=request.ContentType==LiveSessionContentType.Poll ? request.ContentId : (long?)null,
            PackId=request.ContentType==LiveSessionContentType.PollPack ? request.ContentId : (long?)null,
            Code=code, Now=utcNow, Expires=utcNow.AddMinutes(request.Configuration.SessionDurationMinutes)
        }, tx);
        for (var i=0; i<polls.Count; i++)
            await conn.ExecuteAsync("INSERT dbo.LiveSessionRounds(SessionId,RoundNumber,PollId,Status,RulesSnapshot) VALUES(@Id,@Number,@Poll,'Pending',@Rules)",
                new { Id=id, Number=i+1, Poll=polls[i], Rules=JsonSerializer.Serialize(new { request.Configuration.RoundDurationSeconds }) }, tx);
        await AddEvent(conn, tx, id, "session.created", hostUserId, new { request.Mode, request.ContentType }, utcNow);
        tx.Commit();
        return (await GetAsync(id, hostUserId))!;
    }

    public async Task<LiveSessionDto?> GetAsync(long id, long callerUserId)
    {
        using var conn=(SqlConnection)context.CreateConnection(); await conn.OpenAsync();
        if (!await CanRead(conn,id,callerUserId)) return null;
        return await Load(conn,id);
    }

    public async Task<LiveEventReplayDto> GetEventsAsync(long id, long callerUserId, long afterSequence)
    {
        if (afterSequence < 0) throw new LiveSessionException("invalid_sequence", "afterSequence cannot be negative.");
        using var conn=(SqlConnection)context.CreateConnection(); await conn.OpenAsync();
        if (!await CanRead(conn,id,callerUserId)) throw new LiveSessionException("not_found", "Session not found.");
        var version = Encode(await conn.ExecuteScalarAsync<byte[]>("SELECT RowVersion FROM dbo.LiveSessions WHERE Id=@id",new{id})
            ?? throw new LiveSessionException("not_found", "Session not found."));
        var rows=await conn.QueryAsync<EventRow>("SELECT Sequence,EventType,ActorUserId,Payload,SchemaVersion,OccurredAt FROM dbo.LiveSessionEvents WHERE SessionId=@id AND Sequence>@afterSequence ORDER BY Sequence",new{id,afterSequence});
        var events=rows.Select(x=>new LiveSessionEventDto(x.Sequence,x.EventType,x.ActorUserId,JsonDocument.Parse(x.Payload).RootElement.Clone(),x.SchemaVersion,x.OccurredAt)).ToList();
        return new(version, events.Count==0 ? await LatestSequence(conn,id) : events[^1].Sequence, events);
    }

    public Task<LiveSessionDto> JoinAsync(long id,long userId,string expectedVersion,DateTime utcNow) => Mutate(id,userId,expectedVersion,utcNow,false,LiveSessionStatus.Lobby,
        async (c,t,s) => {
            var count=await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.LiveSessionParticipants WHERE SessionId=@id AND Status NOT IN ('Left','Removed')",new{id},t);
            var config=JsonSerializer.Deserialize<LiveModeConfiguration>(s.ModeConfiguration)!;
            if(count>=config.MaxParticipants) throw new LiveSessionException("session_full","The session is full.");
            await c.ExecuteAsync(@"IF EXISTS(SELECT 1 FROM dbo.LiveSessionParticipants WHERE SessionId=@id AND UserId=@userId)
UPDATE dbo.LiveSessionParticipants SET Status='Joined',LastActivityAt=@utcNow,LeftAt=NULL WHERE SessionId=@id AND UserId=@userId
ELSE INSERT dbo.LiveSessionParticipants(SessionId,UserId,Status,JoinedAt,LastActivityAt) VALUES(@id,@userId,'Joined',@utcNow,@utcNow)",new{id,userId,utcNow},t);
        }, "participant.joined", allowUnjoined:true);

    public Task<LiveSessionDto> LeaveAsync(long id,long userId,string expectedVersion,DateTime utcNow) => Mutate(id,userId,expectedVersion,utcNow,false,null,
        async(c,t,s)=> { var n=await c.ExecuteAsync("UPDATE dbo.LiveSessionParticipants SET Status='Left',LeftAt=@utcNow,LastActivityAt=@utcNow WHERE SessionId=@id AND UserId=@userId AND Status NOT IN ('Left','Removed')",new{id,userId,utcNow},t); if(n==0) throw new LiveSessionException("forbidden","You are not an active participant."); },"participant.left");

    public Task<LiveSessionDto> StartAsync(long id,long host,string version,DateTime now) => Mutate(id,host,version,now,true,LiveSessionStatus.Lobby,
        async(c,t,s)=> { LiveSessionRules.RequireTransition(s.Status,LiveSessionStatus.Active); var participants=await c.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.LiveSessionParticipants WHERE SessionId=@id AND Status IN ('Joined','Ready')",new{id},t); if(participants<2) throw new LiveSessionException("insufficient_participants","At least two participants are required."); await c.ExecuteAsync("UPDATE dbo.LiveSessionRounds SET Status='Active',StartsAt=@now,EndsAt=DATEADD(second,@seconds,@now) WHERE Id=(SELECT TOP 1 Id FROM dbo.LiveSessionRounds WHERE SessionId=@id ORDER BY RoundNumber)",new{id,now,seconds=JsonSerializer.Deserialize<LiveModeConfiguration>(s.ModeConfiguration)!.RoundDurationSeconds},t); await c.ExecuteAsync("UPDATE dbo.LiveSessionParticipants SET Status='Active',LastActivityAt=@now WHERE SessionId=@id AND Status IN ('Joined','Ready')",new{id,now},t); },"session.started",LiveSessionStatus.Active);

    public async Task<LiveResponseDto> SubmitResponseAsync(long id,long roundId,long userId,SubmitLiveResponseRequest request,DateTime utcNow)
    {
        EnsureUtc(utcNow); using var conn=(SqlConnection)context.CreateConnection(); await conn.OpenAsync(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var s=await Locked(conn,tx,id);
        var p=await conn.QueryFirstOrDefaultAsync<ParticipantRow>("SELECT Id,Status FROM dbo.LiveSessionParticipants WITH(UPDLOCK,HOLDLOCK) WHERE SessionId=@id AND UserId=@userId",new{id,userId},tx);
        if(p is null) throw new LiveSessionException("forbidden","Only a participant may respond.");
        var existing=await conn.QueryFirstOrDefaultAsync<ResponseRow>("SELECT * FROM dbo.LiveSessionResponses WHERE RoundId=@roundId AND ParticipantId=@pid",new{roundId,pid=p.Id},tx);
        if(existing is not null) { tx.Commit(); if(existing.OptionId!=request.OptionId) throw new LiveSessionException("response_conflict","A different response was already accepted."); return Map(existing); }
        RequireVersion(s,request.Version); RequireLive(s,utcNow);
        if(p.Status!="Active") throw new LiveSessionException("forbidden","Only an active participant may respond.");
        var round=await conn.QueryFirstOrDefaultAsync<RoundRow>("SELECT Id,PollId,Status,EndsAt FROM dbo.LiveSessionRounds WITH(UPDLOCK,HOLDLOCK) WHERE Id=@roundId AND SessionId=@id",new{id,roundId},tx);
        if(round is null || round.Status!="Active" || round.EndsAt<=utcNow) throw new LiveSessionException("invalid_transition","The round is not accepting responses.");
        if(!await conn.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.PollOptions WHERE Id=@OptionId AND PollId=@PollId) THEN 1 ELSE 0 END",new{request.OptionId,round.PollId},tx)) throw new LiveSessionException("invalid_option","That option does not belong to the round poll.");
        var responseId=await conn.ExecuteScalarAsync<long>("INSERT dbo.LiveSessionResponses(SessionId,RoundId,ParticipantId,PollId,OptionId,SubmittedAt) OUTPUT INSERTED.Id VALUES(@id,@roundId,@pid,@poll,@option,@utcNow)",new{id,roundId,pid=p.Id,poll=round.PollId,option=request.OptionId,utcNow},tx);
        await Touch(conn,tx,id,request.Version,utcNow); await AddEvent(conn,tx,id,"response.accepted",userId,new{roundId,responseId},utcNow); tx.Commit();
        return new(responseId,roundId,p.Id,round.PollId,request.OptionId,utcNow);
    }

    public Task<LiveSessionDto> CompleteRoundAsync(long id,long roundId,long host,string version,DateTime now) => Mutate(id,host,version,now,true,LiveSessionStatus.Active,
        async(c,t,s)=> { var n=await c.ExecuteAsync("UPDATE dbo.LiveSessionRounds SET Status='Completed',CompletedAt=@now WHERE Id=@roundId AND SessionId=@id AND Status='Active'",new{id,roundId,now},t); if(n==0) throw new LiveSessionException("invalid_transition","Round is not active."); var next=await c.QueryFirstOrDefaultAsync<long?>("SELECT TOP 1 Id FROM dbo.LiveSessionRounds WHERE SessionId=@id AND Status='Pending' ORDER BY RoundNumber",new{id},t); if(next is not null) await c.ExecuteAsync("UPDATE dbo.LiveSessionRounds SET Status='Active',StartsAt=@now,EndsAt=DATEADD(second,@seconds,@now) WHERE Id=@next",new{next,now,seconds=JsonSerializer.Deserialize<LiveModeConfiguration>(s.ModeConfiguration)!.RoundDurationSeconds},t); },"round.completed");

    public async Task<LiveSessionDto> CompleteAsync(long id,long host,string version,DateTime now)
    {
        var result=await Mutate(id,host,version,now,true,LiveSessionStatus.Active,(c,t,s)=>Task.CompletedTask,"session.completed",LiveSessionStatus.Completed,"host_completed");
        foreach(var participant in result.Participants.Where(p=>p.Status==LiveParticipantStatus.Active))
        {
            var source=$"{id}:completion:{participant.Id}";
            var grant=await rewards.GrantAsync(new(participant.UserId,RewardRuleCodes.LiveSessionComplete,"live-session",source,now));
            await RecordRewardEvent(id,host,participant.Id,grant,now);
        }
        return (await GetAsync(id,host))!;
    }

    public Task<LiveSessionDto> AbandonAsync(long id,long host,string version,DateTime now) => Mutate(id,host,version,now,true,null,(c,t,s)=>Task.CompletedTask,"session.abandoned",LiveSessionStatus.Abandoned,"host_abandoned");

    public async Task<LiveCleanupResult> CleanupDueAsync(DateTime utcNow)
    {
        EnsureUtc(utcNow); using var conn=(SqlConnection)context.CreateConnection(); await conn.OpenAsync(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var due=(await conn.QueryAsync<CleanupRow>(@"SELECT Id,CASE WHEN ExpiresAt<=@now THEN 'Expired' ELSE 'Abandoned' END Status FROM dbo.LiveSessions WITH(UPDLOCK,HOLDLOCK) WHERE Status IN('Lobby','Active') AND (ExpiresAt<=@now OR LastActivityAt<=@abandon)",new{now=utcNow,abandon=utcNow-LiveSessionRules.AbandonmentThreshold},tx)).ToList();
        foreach(var row in due) { await conn.ExecuteAsync("UPDATE dbo.LiveSessions SET Status=@Status,CompletedAt=@now,TerminalReason=LOWER(@Status) WHERE Id=@Id AND Status IN('Lobby','Active')",new{row.Id,row.Status,now=utcNow},tx); await AddEvent(conn,tx,row.Id,$"session.{row.Status.ToLowerInvariant()}",null,new{reason=row.Status.ToLowerInvariant()},utcNow); }
        tx.Commit(); return new(due.Count(x=>x.Status=="Expired"),due.Count(x=>x.Status=="Abandoned"));
    }

    private async Task<LiveSessionDto> Mutate(long id,long user,string version,DateTime now,bool hostOnly,LiveSessionStatus? required,Func<SqlConnection,SqlTransaction,SessionRow,Task> action,string eventType,LiveSessionStatus? target=null,string? reason=null,bool allowUnjoined=false)
    {
        EnsureUtc(now); using var conn=(SqlConnection)context.CreateConnection(); await conn.OpenAsync(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var s=await Locked(conn,tx,id); RequireVersion(s,version); RequireLive(s,now);
        if(hostOnly ? s.HostUserId!=user : !allowUnjoined && s.HostUserId!=user && !await IsParticipant(conn,tx,id,user)) throw new LiveSessionException("forbidden","You do not control this session.");
        if(required is not null && s.Status!=required) throw new LiveSessionException("invalid_transition",$"Session must be {required}.");
        if(target is not null) LiveSessionRules.RequireTransition(s.Status,target.Value);
        await action(conn,tx,s);
        var affected=await conn.ExecuteAsync(@"UPDATE dbo.LiveSessions SET Status=COALESCE(@Target,Status),CompletedAt=CASE WHEN @Target IN('Completed','Expired','Abandoned') THEN @Now ELSE CompletedAt END,StartedAt=CASE WHEN @Target='Active' THEN @Now ELSE StartedAt END,TerminalReason=COALESCE(@Reason,TerminalReason),LastActivityAt=@Now WHERE Id=@Id AND RowVersion=@Version",new{Id=id,Target=target?.ToString(),Now=now,Reason=reason,Version=Decode(version)},tx);
        if(affected!=1) throw new LiveSessionException("stale_version","The session was modified by another request.");
        await AddEvent(conn,tx,id,eventType,user,new{},now); tx.Commit(); return (await GetAsync(id,user))!;
    }

    private static async Task<List<long>> EligiblePolls(SqlConnection c,SqlTransaction t,long host,CreateLiveSessionRequest r,DateTime now)
    {
        const string eligible="p.IsActive=1 AND p.ExpiresAt>@now AND p.ModerationStatus='Published' AND ISNULL(p.IsPrivate,0)=0 AND ISNULL(p.IsWellness,0)=0 AND ISNULL(p.IsSponsored,0)=0";
        if(r.ContentType==LiveSessionContentType.Poll) return (await c.QueryAsync<long>($"SELECT p.Id FROM dbo.Polls p WHERE p.Id=@id AND {eligible}",new{id=r.ContentId,now},t)).ToList();
        return (await c.QueryAsync<long>($"SELECT p.Id FROM dbo.PollPacks pp JOIN dbo.PollPackPolls x ON x.PollPackId=pp.Id JOIN dbo.Polls p ON p.Id=x.PollId WHERE pp.Id=@id AND pp.IsPublished=1 AND pp.IsActive=1 AND (pp.IsPublic=1 OR pp.OwnerUserId=@host) AND {eligible} ORDER BY x.Position",new{id=r.ContentId,host,now},t)).ToList();
    }
    private static async Task<string> UniqueJoinCode(SqlConnection c,SqlTransaction t) { for(var i=0;i<10;i++){var code=Convert.ToHexString(RandomNumberGenerator.GetBytes(4));if(!await c.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.LiveSessions WHERE JoinCode=@code) THEN 1 ELSE 0 END",new{code},t))return code;} throw new LiveSessionException("join_code_unavailable","Could not allocate a join code."); }
    private static async Task<SessionRow> Locked(SqlConnection c,SqlTransaction t,long id)=>await c.QueryFirstOrDefaultAsync<SessionRow>("SELECT * FROM dbo.LiveSessions WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",new{id},t)??throw new LiveSessionException("not_found","Session not found.");
    private static void RequireVersion(SessionRow s,string v) { byte[] decoded; try{decoded=Decode(v);}catch{throw new LiveSessionException("invalid_version","Version token is invalid.");} if(!s.RowVersion.SequenceEqual(decoded))throw new LiveSessionException("stale_version","The session was modified by another request."); }
    private static void RequireLive(SessionRow s,DateTime now){if(LiveSessionRules.IsTerminal(s.Status))throw new LiveSessionException("invalid_transition","The session is terminal.");if(LiveSessionRules.IsExpired(s.ExpiresAt,now))throw new LiveSessionException("session_expired","The session has expired.");}
    private static Task<bool> IsParticipant(SqlConnection c,SqlTransaction? t,long id,long user)=>c.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.LiveSessionParticipants WHERE SessionId=@id AND UserId=@user AND Status NOT IN('Left','Removed')) THEN 1 ELSE 0 END",new{id,user},t);
    private static async Task<bool> CanRead(SqlConnection c,long id,long user)=>await c.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.LiveSessions s WHERE s.Id=@id AND (s.HostUserId=@user OR EXISTS(SELECT 1 FROM dbo.LiveSessionParticipants p WHERE p.SessionId=s.Id AND p.UserId=@user AND p.Status NOT IN('Left','Removed')))) THEN 1 ELSE 0 END",new{id,user});
    private static async Task Touch(SqlConnection c,SqlTransaction t,long id,string version,DateTime now){if(await c.ExecuteAsync("UPDATE dbo.LiveSessions SET LastActivityAt=@now WHERE Id=@id AND RowVersion=@v",new{id,now,v=Decode(version)},t)!=1)throw new LiveSessionException("stale_version","The session was modified by another request.");}
    private static async Task AddEvent(SqlConnection c,SqlTransaction t,long id,string type,long? actor,object payload,DateTime now){var seq=await c.ExecuteScalarAsync<long>("SELECT ISNULL(MAX(Sequence),0)+1 FROM dbo.LiveSessionEvents WITH(UPDLOCK,HOLDLOCK) WHERE SessionId=@id",new{id},t);await c.ExecuteAsync("INSERT dbo.LiveSessionEvents(SessionId,Sequence,EventType,ActorUserId,Payload,SchemaVersion,OccurredAt) VALUES(@id,@seq,@type,@actor,@payload,1,@now)",new{id,seq,type,actor,payload=JsonSerializer.Serialize(payload),now},t);}
    private static async Task<long> LatestSequence(SqlConnection c,long id)=>await c.ExecuteScalarAsync<long>("SELECT ISNULL(MAX(Sequence),0) FROM dbo.LiveSessionEvents WHERE SessionId=@id",new{id});
    private async Task<LiveSessionDto> Load(SqlConnection c,long id){var s=await c.QuerySingleAsync<SessionRow>("SELECT * FROM dbo.LiveSessions WHERE Id=@id",new{id});var ps=(await c.QueryAsync<ParticipantFull>("SELECT Id,UserId,Status,JoinedAt FROM dbo.LiveSessionParticipants WHERE SessionId=@id ORDER BY Id",new{id})).Select(x=>new LiveParticipantDto(x.Id,x.UserId,Enum.Parse<LiveParticipantStatus>(x.Status),x.JoinedAt)).ToList();var rs=(await c.QueryAsync<RoundFull>("SELECT * FROM dbo.LiveSessionRounds WHERE SessionId=@id ORDER BY RoundNumber",new{id})).Select(x=>new LiveRoundDto(x.Id,x.RoundNumber,x.PollId,Enum.Parse<LiveRoundStatus>(x.Status),x.StartsAt,x.EndsAt,x.CompletedAt)).ToList();return new(){Id=s.Id,HostUserId=s.HostUserId,Mode=Enum.Parse<LiveGameMode>(s.Mode),Configuration=JsonSerializer.Deserialize<LiveModeConfiguration>(s.ModeConfiguration)!,ContentType=Enum.Parse<LiveSessionContentType>(s.ContentType),ContentId=s.PollId??s.PollPackId!.Value,Status=s.Status,JoinCode=s.JoinCode,CreatedAt=s.CreatedAt,StartedAt=s.StartedAt,LastActivityAt=s.LastActivityAt,ExpiresAt=s.ExpiresAt,CompletedAt=s.CompletedAt,TerminalReason=s.TerminalReason,Version=Encode(s.RowVersion),LatestEventSequence=await LatestSequence(c,id),Participants=ps,Rounds=rs};}
    private async Task RecordRewardEvent(long id,long actor,long participant,RewardGrantResult grant,DateTime now){using var c=(SqlConnection)context.CreateConnection();await c.OpenAsync();using var t=c.BeginTransaction(IsolationLevel.Serializable);await AddEvent(c,t,id,grant.IsDuplicate?"reward.duplicate":"reward.granted",actor,new{participant,rewardEventId=grant.Event.Id},now);t.Commit();}
    private static LiveResponseDto Map(ResponseRow x)=>new(x.Id,x.RoundId,x.ParticipantId,x.PollId,x.OptionId,x.SubmittedAt);
    private static string Encode(byte[] v)=>Convert.ToBase64String(v); private static byte[] Decode(string v)=>Convert.FromBase64String(v);
    private static void EnsureUtc(DateTime value){if(value.Kind!=DateTimeKind.Utc)throw new ArgumentException("Timestamps must be UTC.");}
    private sealed class SessionRow{public long Id{get;set;}public long HostUserId{get;set;}public string Mode{get;set;}="";public string ModeConfiguration{get;set;}="";public string ContentType{get;set;}="";public long? PollId{get;set;}public long? PollPackId{get;set;}public LiveSessionStatus Status{get;set;}public string JoinCode{get;set;}="";public DateTime CreatedAt{get;set;}public DateTime? StartedAt{get;set;}public DateTime LastActivityAt{get;set;}public DateTime ExpiresAt{get;set;}public DateTime? CompletedAt{get;set;}public string? TerminalReason{get;set;}public byte[] RowVersion{get;set;}=[];}
    private sealed class EventRow{public long Sequence{get;set;}public string EventType{get;set;}="";public long? ActorUserId{get;set;}public string Payload{get;set;}="{}";public int SchemaVersion{get;set;}public DateTime OccurredAt{get;set;}}
    private sealed class ParticipantRow{public long Id{get;set;}public string Status{get;set;}="";} private sealed class ParticipantFull{public long Id{get;set;}public long UserId{get;set;}public string Status{get;set;}="";public DateTime JoinedAt{get;set;}}
    private sealed class RoundRow{public long Id{get;set;}public long PollId{get;set;}public string Status{get;set;}="";public DateTime? EndsAt{get;set;}} private sealed class RoundFull{public long Id{get;set;}public int RoundNumber{get;set;}public long PollId{get;set;}public string Status{get;set;}="";public DateTime? StartsAt{get;set;}public DateTime? EndsAt{get;set;}public DateTime? CompletedAt{get;set;}}
    private sealed class ResponseRow{public long Id{get;set;}public long RoundId{get;set;}public long ParticipantId{get;set;}public long PollId{get;set;}public long OptionId{get;set;}public DateTime SubmittedAt{get;set;}} private sealed class CleanupRow{public long Id{get;set;}public string Status{get;set;}="";}
}
