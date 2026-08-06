using System.Security.Cryptography;
using System.Text;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository;

public sealed class MultiplayerTrustRepository(DapperContext context) : IMultiplayerTrustRepository
{
    public async Task<SafetyReportReceipt> CreateReportAsync(long? userId, Guid? participantId, CreateSafetyReportRequest r)
    {
        if (userId is null && participantId is null) throw new UnauthorizedAccessException("Participant capability required.");
        var comment = r.Comment?.Trim();
        if (comment?.Length > 500) throw new ArgumentException("Comment must not exceed 500 characters.");
        using var db = context.CreateConnection(); db.Open(); using var tx = db.BeginTransaction();
        var reporterValid = await db.ExecuteScalarAsync<bool>(@"SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM LiveSessionParticipants p JOIN LiveSessions s ON s.Id=p.SessionId WHERE s.PublicId=@Session AND ((@User IS NOT NULL AND p.UserId=@User) OR (@Participant IS NOT NULL AND p.PublicId=@Participant))) THEN 1 ELSE 0 END AS bit)", new { Session=r.SessionId, User=userId, Participant=participantId }, tx);
        if (!reporterValid) throw new UnauthorizedAccessException("Session participation required.");
        var targetValid = r.TargetType switch {
            SafetyTargetType.Session => true,
            SafetyTargetType.Participant => r.ParticipantId.HasValue && await db.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM LiveSessionParticipants p JOIN LiveSessions s ON s.Id=p.SessionId WHERE s.PublicId=@Session AND p.PublicId=@Target) THEN 1 ELSE 0 END AS bit)", new { Session=r.SessionId, Target=r.ParticipantId }, tx),
            SafetyTargetType.Poll => r.PollId.HasValue && await db.ExecuteScalarAsync<bool>("SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM LiveSessionPolls sp JOIN LiveSessions s ON s.Id=sp.SessionId WHERE s.PublicId=@Session AND sp.PollId=@Poll) THEN 1 ELSE 0 END AS bit)", new { Session=r.SessionId, Poll=r.PollId }, tx),
            _ => false
        };
        if (!targetValid) throw new ArgumentException("Report target is unavailable.");
        var receipt = Guid.NewGuid();
        var id = await db.ExecuteScalarAsync<long>(@"INSERT SafetyReports(ReceiptId,ReporterUserId,ReporterParticipantId,TargetType,SessionId,TargetParticipantId,PollId,ReasonCode,Comment) OUTPUT inserted.Id VALUES(@Receipt,@User,@Participant,@Type,@Session,@Target,@Poll,@Reason,@Comment)", new { Receipt=receipt, User=userId, Participant=participantId, Type=r.TargetType.ToString(), Session=r.SessionId, Target=r.ParticipantId, Poll=r.PollId, Reason=r.Reason.ToString(), Comment=comment }, tx);
        await db.ExecuteAsync("INSERT SafetyReportAuditEvents(ReportId,Action,NewStatus) VALUES(@Id,'Created','Open')", new { Id=id }, tx);
        tx.Commit(); return new(receipt, "Open", DateTime.UtcNow);
    }

    public async Task LeaveAsync(Guid sessionId, long? userId, string? token)
    {
        using var db=context.CreateConnection(); var hash=token is null?null:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var changed=await db.ExecuteAsync(@"UPDATE p SET State='Left',LeftAt=SYSUTCDATETIME(),ReconnectTokenHash=NULL,NotificationsEnabled=0 FROM LiveSessionParticipants p JOIN LiveSessions s ON s.Id=p.SessionId WHERE s.PublicId=@Session AND p.State='Joined' AND ((@User IS NOT NULL AND p.UserId=@User) OR (@Hash IS NOT NULL AND p.ReconnectTokenHash=@Hash))",new{Session=sessionId,User=userId,Hash=hash});
        if(changed==0) throw new KeyNotFoundException("Membership unavailable.");
    }
    public async Task<MultiplayerPrivacySettings> GetPrivacyAsync(long userId){using var db=context.CreateConnection();return await db.QuerySingleOrDefaultAsync<MultiplayerPrivacySettings>("SELECT DiscloseIdentity,DiscloseIndividualVote,ShareCoarseRegion,AllowPublicResultCard FROM MultiplayerPrivacySettings WHERE UserId=@User",new{User=userId}) ?? new();}
    public async Task SavePrivacyAsync(long userId,MultiplayerPrivacySettings s){using var db=context.CreateConnection();await db.ExecuteAsync(@"MERGE MultiplayerPrivacySettings t USING(SELECT @User UserId)s ON t.UserId=s.UserId WHEN MATCHED THEN UPDATE SET DiscloseIdentity=@DiscloseIdentity,DiscloseIndividualVote=@DiscloseIndividualVote,ShareCoarseRegion=@ShareCoarseRegion,AllowPublicResultCard=@AllowPublicResultCard,UpdatedAt=SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT(UserId,DiscloseIdentity,DiscloseIndividualVote,ShareCoarseRegion,AllowPublicResultCard) VALUES(@User,@DiscloseIdentity,@DiscloseIndividualVote,@ShareCoarseRegion,@AllowPublicResultCard);",new{User=userId,s.DiscloseIdentity,s.DiscloseIndividualVote,s.ShareCoarseRegion,s.AllowPublicResultCard});}
    public async Task<MultiplayerNotificationSettings> GetNotificationsAsync(long userId){using var db=context.CreateConnection();return await db.QuerySingleOrDefaultAsync<MultiplayerNotificationSettings>("SELECT Invitations,SessionActivity,Reminders,Results,QuietHoursStart,QuietHoursEnd,TimeZoneId,AllowCritical FROM MultiplayerNotificationSettings WHERE UserId=@User",new{User=userId}) ?? new();}
    public async Task SaveNotificationsAsync(long userId,MultiplayerNotificationSettings s){_ = TimeZoneInfo.FindSystemTimeZoneById(s.TimeZoneId);using var db=context.CreateConnection();await db.ExecuteAsync(@"MERGE MultiplayerNotificationSettings t USING(SELECT @User UserId)s ON t.UserId=s.UserId WHEN MATCHED THEN UPDATE SET Invitations=@Invitations,SessionActivity=@SessionActivity,Reminders=@Reminders,Results=@Results,QuietHoursStart=@QuietHoursStart,QuietHoursEnd=@QuietHoursEnd,TimeZoneId=@TimeZoneId,AllowCritical=@AllowCritical,UpdatedAt=SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT(UserId,Invitations,SessionActivity,Reminders,Results,QuietHoursStart,QuietHoursEnd,TimeZoneId,AllowCritical) VALUES(@User,@Invitations,@SessionActivity,@Reminders,@Results,@QuietHoursStart,@QuietHoursEnd,@TimeZoneId,@AllowCritical);",new{User=userId,s.Invitations,s.SessionActivity,s.Reminders,s.Results,s.QuietHoursStart,s.QuietHoursEnd,s.TimeZoneId,s.AllowCritical});}
}
