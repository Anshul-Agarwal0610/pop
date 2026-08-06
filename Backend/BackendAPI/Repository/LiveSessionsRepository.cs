using System.Data;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace BackendAPI.Repository;

public sealed class LiveSessionsRepository(DapperContext context, IOptions<PollBombOptions> configured,
    ILiveSessionNotifier notifier) : ILiveSessionsRepository
{
    private readonly PollBombOptions options = configured.Value;

    public async Task<LiveSessionStateDto> CreateAsync(long userId, CreateLiveSessionRequest request, DateTime now)
    {
        if (request.Mode != LiveGameMode.Bomb || !PollBombRules.IsApproved(options, request.TargetVotes, request.DurationSeconds, request.ExpiryPolicy))
            throw new LiveSessionException("invalid_configuration", "Choose a server-approved target, duration, and expiry policy.");
        using var conn = context.CreateConnection(); conn.Open(); using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var pollExists = await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(1) FROM Polls WITH (HOLDLOCK) WHERE Id=@PollId AND IsActive=1 AND ExpiresAt>@Now AND ModerationStatus='Published'", new { request.PollId, Now = now }, tx) == 1;
        if (!pollExists) throw new LiveSessionException("poll_unavailable", "That poll is unavailable.");
        var publicId = Guid.NewGuid().ToString("N");
        var capacity = PollBombRules.Capacity(options, request.TargetVotes);
        var id = await conn.ExecuteScalarAsync<long>(@"INSERT INTO LiveSessions(PublicId,HostUserId,Mode,Status,PollId,TargetVoteCount,DurationSeconds,Capacity,ExpiryPolicy,ExpiresAt,CreatedAt,UpdatedAt)
            OUTPUT inserted.Id VALUES(@PublicId,@UserId,'Bomb','Voting',@PollId,@TargetVotes,@Duration,@Capacity,'ExpireWithoutReveal',@ExpiresAt,@Now,@Now)",
            new { PublicId=publicId, UserId=userId, request.PollId, request.TargetVotes, Duration=request.DurationSeconds, Capacity=capacity, ExpiresAt=now.AddSeconds(request.DurationSeconds), Now=now }, tx);
        await conn.ExecuteAsync(@"INSERT INTO LiveSessionParticipants(SessionId,UserId,Status,NotificationsEnabled,JoinedAt) VALUES(@Id,@UserId,'Active',@Notify,@Now);
            INSERT INTO LiveSessionEvents(SessionId,Sequence,Type,StateVersion,Payload,CreatedAt) VALUES(@Id,1,'BombCreated',1,'{}',@Now)", new { Id=id, UserId=userId, Notify=request.NotificationsEnabled, Now=now }, tx);
        tx.Commit();
        return (await GetAsync(publicId, userId, now))!;
    }

    public async Task<LiveSessionStateDto?> GetAsync(string publicId, long userId, DateTime now)
    {
        await ExpireOneAsync(publicId, now);
        using var conn=context.CreateConnection(); conn.Open();
        return await ProjectAsync(conn, publicId, userId, now);
    }

    public async Task<LiveSessionStateDto> JoinAsync(string publicId, long userId, DateTime now)
    {
        await ExpireOneAsync(publicId, now);
        using var conn=context.CreateConnection(); conn.Open(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var s=await LockSession(conn,tx,publicId) ?? throw NotFound();
        EnsureVoting(s);
        var existing=await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM LiveSessionParticipants WITH (UPDLOCK,HOLDLOCK) WHERE SessionId=@Id AND UserId=@UserId",new { Id=(long)s.Id, UserId=userId },tx);
        if (existing is null)
        {
            var joined=await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM LiveSessionParticipants WHERE SessionId=@Id AND Status='Active'",new { Id=(long)s.Id },tx);
            if(joined >= (int)s.Capacity) throw new LiveSessionException("capacity_reached","This Poll Bomb is full.");
            await conn.ExecuteAsync("INSERT INTO LiveSessionParticipants(SessionId,UserId,Status,JoinedAt) VALUES(@Id,@UserId,'Active',@Now)",new { Id=(long)s.Id,UserId=userId,Now=now},tx);
        }
        else if ((string)existing.Status == "Removed")
            await conn.ExecuteAsync("UPDATE LiveSessionParticipants SET Status='Active',JoinedAt=@Now,RemovedAt=NULL WHERE Id=@ParticipantId",new { Now=now,ParticipantId=(long)existing.Id},tx);
        await BumpAndEvent(conn,tx,(long)s.Id,"ParticipantJoined",now);
        tx.Commit(); return (await GetAsync(publicId,userId,now))!;
    }

    public async Task<LiveSessionStateDto> VoteAsync(string publicId,long userId,LockLiveSessionVoteRequest request,DateTime now)
    {
        LiveSessionEventDto? reveal=null;
        using var conn=context.CreateConnection(); conn.Open(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var s=await LockSession(conn,tx,publicId) ?? throw NotFound();
        if(PollBombRules.IsExpired((DateTime)s.ExpiresAt,now)){ await ExpireLocked(conn,tx,s,now); tx.Commit(); throw new LiveSessionException("expired","This Poll Bomb expired without a reveal."); }
        EnsureVoting(s);
        var participant=await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM LiveSessionParticipants WITH (UPDLOCK,HOLDLOCK) WHERE SessionId=@Id AND UserId=@UserId AND Status='Active'",new {Id=(long)s.Id,UserId=userId},tx) ?? throw new LiveSessionException("not_joined","Join this Poll Bomb before voting.");
        var validOption=await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PollOptions WHERE Id=@OptionId AND PollId=@PollId",new {request.OptionId,PollId=(long)s.PollId},tx)==1;
        if(!validOption) throw new LiveSessionException("invalid_option","That option does not belong to this poll.");
        var prior=await conn.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM LiveSessionResponses WHERE SessionId=@Id AND ParticipantId=@ParticipantId",new {Id=(long)s.Id,ParticipantId=(long)participant.Id},tx);
        if(prior is not null)
        {
            if((long)prior.OptionId==request.OptionId && (string)prior.IdempotencyKey==request.IdempotencyKey){ tx.Commit(); return (await GetAsync(publicId,userId,now))!; }
            throw new LiveSessionException("vote_locked","A locked vote cannot be changed.");
        }
        try { await conn.ExecuteAsync("INSERT INTO LiveSessionResponses(SessionId,ParticipantId,OptionId,IdempotencyKey,LockedAt) VALUES(@Id,@ParticipantId,@OptionId,@Key,@Now)",new {Id=(long)s.Id,ParticipantId=(long)participant.Id,request.OptionId,Key=request.IdempotencyKey,Now=now},tx); }
        catch(SqlException ex) when(ex.Number is 2601 or 2627){ throw new LiveSessionException("duplicate_request","That vote request was already used."); }
        var count=await ValidCount(conn,tx,(long)s.Id);
        if(PollBombRules.ShouldReveal(LiveSessionStatus.Voting,count,(int)s.TargetVoteCount))
        {
            await conn.ExecuteAsync("UPDATE LiveSessions SET Status='Revealed',ValidLockedVoteCount=@Count,RevealedAt=@Now,TerminalReason='ThresholdReached',StateVersion=StateVersion+1,UpdatedAt=@Now WHERE Id=@Id AND Status='Voting'",new {Id=(long)s.Id,Count=count,Now=now},tx);
            reveal=await InsertEvent(conn,tx,(long)s.Id,"BombRevealed",now);
        }
        else { await conn.ExecuteAsync("UPDATE LiveSessions SET ValidLockedVoteCount=@Count,StateVersion=StateVersion+1,UpdatedAt=@Now WHERE Id=@Id",new {Id=(long)s.Id,Count=count,Now=now},tx); await InsertEvent(conn,tx,(long)s.Id,"VoteLocked",now); }
        tx.Commit();
        if(reveal is not null) await notifier.StateChangedAsync(publicId,reveal);
        return (await GetAsync(publicId,userId,now))!;
    }

    public async Task<LiveSessionStateDto> RemoveAsync(string publicId,long hostUserId,long participantId,DateTime now)
    {
        using var conn=context.CreateConnection(); conn.Open(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var s=await LockSession(conn,tx,publicId) ?? throw NotFound();
        if((long)s.HostUserId!=hostUserId) throw new LiveSessionException("forbidden","Only the host can remove participants.");
        EnsureVoting(s);
        var changed=await conn.ExecuteAsync("UPDATE LiveSessionParticipants SET Status='Removed',RemovedAt=@Now WHERE Id=@ParticipantId AND SessionId=@Id AND Status='Active' AND UserId<>@Host",new {ParticipantId=participantId,Id=(long)s.Id,Host=hostUserId,Now=now},tx);
        if(changed==0) throw new LiveSessionException("participant_not_found","Participant not found.");
        await conn.ExecuteAsync("DELETE FROM LiveSessionResponses WHERE SessionId=@Id AND ParticipantId=@ParticipantId",new {Id=(long)s.Id,ParticipantId=participantId},tx);
        var count=await ValidCount(conn,tx,(long)s.Id);
        await conn.ExecuteAsync("UPDATE LiveSessions SET ValidLockedVoteCount=@Count,StateVersion=StateVersion+1,UpdatedAt=@Now WHERE Id=@Id",new {Id=(long)s.Id,Count=count,Now=now},tx);
        await InsertEvent(conn,tx,(long)s.Id,"ParticipantRemoved",now); tx.Commit(); return (await GetAsync(publicId,hostUserId,now))!;
    }

    public async Task<LiveSessionStateDto> SetNotificationsAsync(string publicId,long userId,bool enabled,DateTime now)
    {
        using var conn=context.CreateConnection();
        var changed=await conn.ExecuteAsync(@"UPDATE p SET NotificationsEnabled=@Enabled FROM LiveSessionParticipants p JOIN LiveSessions s ON s.Id=p.SessionId WHERE s.PublicId=@PublicId AND p.UserId=@UserId AND p.Status='Active' AND s.Status='Voting'",new {Enabled=enabled,PublicId=publicId,UserId=userId});
        if(changed==0) throw NotFound(); return (await GetAsync(publicId,userId,now))!;
    }

    public async Task<IReadOnlyList<LiveSessionEventDto>> EventsAsync(string publicId,long userId,long afterSequence,DateTime now)
    {
        if(await GetAsync(publicId,userId,now) is null) throw NotFound(); using var conn=context.CreateConnection();
        return (await conn.QueryAsync<LiveSessionEventDto>(@"SELECT e.Sequence,e.Type,e.StateVersion,e.Payload,e.CreatedAt FROM LiveSessionEvents e JOIN LiveSessions s ON s.Id=e.SessionId WHERE s.PublicId=@PublicId AND e.Sequence>@After ORDER BY e.Sequence",new {PublicId=publicId,After=afterSequence})).AsList();
    }

    public async Task<int> ExpireDueAsync(DateTime now)
    {
        using var conn=context.CreateConnection(); conn.Open(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var ids=(await conn.QueryAsync<long>("SELECT Id FROM LiveSessions WITH (UPDLOCK,READPAST,ROWLOCK) WHERE Status='Voting' AND ExpiresAt<=@Now",new {Now=now},tx)).ToList();
        foreach(var id in ids){ var s=await conn.QuerySingleAsync<dynamic>("SELECT * FROM LiveSessions WHERE Id=@Id",new {Id=id},tx); await ExpireLocked(conn,tx,s,now); }
        tx.Commit(); return ids.Count;
    }

    private async Task ExpireOneAsync(string publicId,DateTime now){ using var conn=context.CreateConnection();conn.Open();using var tx=conn.BeginTransaction(IsolationLevel.Serializable);var s=await LockSession(conn,tx,publicId);if(s is not null&&(string)s.Status=="Voting"&&PollBombRules.IsExpired((DateTime)s.ExpiresAt,now))await ExpireLocked(conn,tx,s,now);tx.Commit(); }
    private static async Task ExpireLocked(IDbConnection c,IDbTransaction t,dynamic s,DateTime now){ if((string)s.Status!="Voting")return;await c.ExecuteAsync("UPDATE LiveSessions SET Status='Expired',TerminalReason='TargetNotReached',StateVersion=StateVersion+1,UpdatedAt=@Now WHERE Id=@Id AND Status='Voting'",new {Id=(long)s.Id,Now=now},t);await InsertEvent(c,t,(long)s.Id,"BombExpired",now); }
    private static Task<dynamic?> LockSession(IDbConnection c,IDbTransaction t,string publicId)=>c.QuerySingleOrDefaultAsync<dynamic>("SELECT * FROM LiveSessions WITH (UPDLOCK,HOLDLOCK,ROWLOCK) WHERE PublicId=@PublicId",new {PublicId=publicId},t);
    private static void EnsureVoting(dynamic s){ if((string)s.Status!="Voting")throw new LiveSessionException("terminal",$"This Poll Bomb is {(string)s.Status}."); }
    private static LiveSessionException NotFound()=>new("not_found","Poll Bomb not found.");
    private static Task<int> ValidCount(IDbConnection c,IDbTransaction t,long id)=>c.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM LiveSessionResponses r JOIN LiveSessionParticipants p ON p.Id=r.ParticipantId WHERE r.SessionId=@Id AND p.Status='Active'",new {Id=id},t);
    private static async Task<LiveSessionEventDto> InsertEvent(IDbConnection c,IDbTransaction t,long id,string type,DateTime now){var version=await c.ExecuteScalarAsync<int>("SELECT StateVersion FROM LiveSessions WHERE Id=@Id",new{Id=id},t);var seq=await c.ExecuteScalarAsync<long>("SELECT ISNULL(MAX(Sequence),0)+1 FROM LiveSessionEvents WITH (UPDLOCK,HOLDLOCK) WHERE SessionId=@Id",new{Id=id},t);await c.ExecuteAsync("INSERT INTO LiveSessionEvents(SessionId,Sequence,Type,StateVersion,Payload,CreatedAt) VALUES(@Id,@Sequence,@Type,@Version,'{}',@Now)",new{Id=id,Sequence=seq,Type=type,Version=version,Now=now},t);return new(){Sequence=seq,Type=type,StateVersion=version,CreatedAt=now};}
    private static async Task BumpAndEvent(IDbConnection c,IDbTransaction t,long id,string type,DateTime now){await c.ExecuteAsync("UPDATE LiveSessions SET StateVersion=StateVersion+1,UpdatedAt=@Now WHERE Id=@Id",new{Id=id,Now=now},t);await InsertEvent(c,t,id,type,now);}

    private static async Task<LiveSessionStateDto?> ProjectAsync(IDbConnection c,string publicId,long userId,DateTime now)
    {
        var row=await c.QuerySingleOrDefaultAsync<dynamic>(@"SELECT s.PublicId,s.Mode,s.Status,s.HostUserId,p.Id ParticipantId,CAST(CASE WHEN s.HostUserId=@UserId THEN 1 ELSE 0 END AS bit) IsHost,
            CAST(CASE WHEN r.Id IS NULL THEN 0 ELSE 1 END AS bit) HasLockedVote,p.NotificationsEnabled,
            (SELECT COUNT(*) FROM LiveSessionParticipants x WHERE x.SessionId=s.Id AND x.Status='Active') JoinedCount,s.ValidLockedVoteCount LockedCount,s.TargetVoteCount TargetVotes,s.StateVersion,
            @Now ServerNow,s.ExpiresAt,s.RevealedAt,s.TerminalReason,s.PollId,q.Question
            FROM LiveSessions s JOIN LiveSessionParticipants p ON p.SessionId=s.Id AND p.UserId=@UserId AND p.Status='Active'
            JOIN Polls q ON q.Id=s.PollId LEFT JOIN LiveSessionResponses r ON r.SessionId=s.Id AND r.ParticipantId=p.Id WHERE s.PublicId=@PublicId",new{PublicId=publicId,UserId=userId,Now=now});
        if(row is null)return null;
        var state=new LiveSessionStateDto { PublicId=row.PublicId,Mode=Enum.Parse<LiveGameMode>((string)row.Mode),Status=Enum.Parse<LiveSessionStatus>((string)row.Status),HostUserId=row.HostUserId,ParticipantId=row.ParticipantId,IsHost=row.IsHost,HasLockedVote=row.HasLockedVote,NotificationsEnabled=row.NotificationsEnabled,JoinedCount=row.JoinedCount,LockedCount=row.LockedCount,TargetVotes=row.TargetVotes,StateVersion=row.StateVersion,ServerNow=row.ServerNow,ExpiresAt=row.ExpiresAt,RevealedAt=row.RevealedAt,TerminalReason=row.TerminalReason,Poll=new(){Id=row.PollId,Question=row.Question} };
        var revealed=state.Status==LiveSessionStatus.Revealed;
        state.Poll.Options=(await c.QueryAsync<LiveSessionOptionDto>(@"SELECT o.Id,o.Text,CASE WHEN @Revealed=1 THEN (SELECT COUNT(*) FROM LiveSessionResponses r JOIN LiveSessionParticipants p ON p.Id=r.ParticipantId WHERE r.SessionId=s.Id AND p.Status='Active' AND r.OptionId=o.Id) END VoteCount FROM LiveSessions s JOIN PollOptions o ON o.PollId=s.PollId WHERE s.PublicId=@PublicId ORDER BY o.Id",new{PublicId=publicId,Revealed=revealed})).AsList();
        return state;
    }
}
