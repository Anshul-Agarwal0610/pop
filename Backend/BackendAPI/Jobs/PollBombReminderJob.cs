using System.Data;
using BackendAPI.Data;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Extensions.Options;

namespace BackendAPI.Jobs;

public sealed class PollBombReminderJob(DapperContext context, ISystemClock clock, IOptions<PollBombOptions> configured)
{
    public async Task<int> RunAsync()
    {
        var now=clock.UtcNow; var options=configured.Value;
        using var conn=context.CreateConnection(); conn.Open(); using var tx=conn.BeginTransaction(IsolationLevel.Serializable);
        var candidates=(await conn.QueryAsync<dynamic>(@"SELECT p.Id ParticipantId,p.UserId,p.ReminderCount,p.LastReminderAt,s.Id SessionId,s.PublicId,s.Status,s.ExpiresAt,
            CAST(CASE WHEN r.Id IS NULL THEN 0 ELSE 1 END AS bit) HasVoted
            FROM LiveSessionParticipants p WITH(UPDLOCK,READPAST,ROWLOCK) JOIN LiveSessions s ON s.Id=p.SessionId
            LEFT JOIN LiveSessionResponses r ON r.SessionId=s.Id AND r.ParticipantId=p.Id
            WHERE p.Status='Active' AND p.NotificationsEnabled=1 AND s.Status='Voting' AND s.ExpiresAt>@Now",new{Now=now},tx)).ToList();
        var sent=0;
        foreach(var c in candidates)
        {
            if(!PollBombRules.ReminderEligible(true,(bool)c.HasVoted,LiveSessionStatus.Voting,(DateTime)c.ExpiresAt,now,(DateTime?)c.LastReminderAt,(int)c.ReminderCount,options))continue;
            var window=(long)Math.Floor((now-new DateTime(1970,1,1)).TotalMinutes/options.ReminderCooldownMinutes);
            var key=$"bomb:{c.PublicId}:reminder:{window}";
            var inserted=await conn.ExecuteAsync(@"IF NOT EXISTS(SELECT 1 FROM Notifications WHERE UserId=@UserId AND DedupKey=@Key)
                INSERT INTO Notifications(UserId,Type,Title,Body,PollId,DedupKey,IsRead,CreatedAt) VALUES(@UserId,'PollBombReminder','Your Poll Bomb is waiting','Lock your vote before the Bomb expires.',NULL,@Key,0,@Now)",new{UserId=(long)c.UserId,Key=key,Now=now},tx);
            if(inserted>0){await conn.ExecuteAsync("UPDATE LiveSessionParticipants SET ReminderCount=ReminderCount+1,LastReminderAt=@Now WHERE Id=@Id",new{Id=(long)c.ParticipantId,Now=now},tx);sent++;}
        }
        tx.Commit(); return sent;
    }
}
