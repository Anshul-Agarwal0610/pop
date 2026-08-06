using System.Data;
using System.Security.Cryptography;
using System.Text;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository;

public sealed class RelayRepository(DapperContext context) : IRelayRepository
{
    private static readonly int[] DefaultMilestones = [3, 5, 10, 25, 50, 100];
    private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
    private static void ValidateMethod(string? method)
    {
        if (method is not ("Link" or "NativeShare" or "CopyLink"))
            throw new RelayDomainException(RelayErrorCodes.Invalid, "Unsupported transfer method.");
    }

    public async Task<RelayStartResult> StartAsync(long userId, StartRelayRequest request, DateTime utcNow)
    {
        RelayRules.ValidateTtl(request.HandoffTtlMinutes);
        RelayRules.ValidateLength(request.MaxLength);
        ValidateMethod(request.TransferMethod);
        var token = NewToken();
        using var db = context.CreateConnection(); db.Open();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);
        var optionCount = await db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM PollOptions o JOIN Polls p ON p.Id=o.PollId
            WHERE p.Id=@PollId AND p.PollMode='Relay' AND p.IsActive=1
              AND p.ModerationStatus='Published' AND p.ExpiresAt>@Now", new { request.PollId, Now=utcNow }, tx);
        if (optionCount != 2) throw new RelayDomainException(RelayErrorCodes.Invalid, "Relay polls must be active, published, and have exactly two options.");
        var chainId = await db.ExecuteScalarAsync<long>(@"
            INSERT RelayChains(PollId,CreatedByUserId,Status,HandoffTtlMinutes,MaxLength,CreatedAt)
            OUTPUT inserted.Id VALUES(@PollId,@UserId,'Active',@Ttl,@MaxLength,@Now)",
            new { request.PollId, UserId=userId, Ttl=request.HandoffTtlMinutes, request.MaxLength, Now=utcNow }, tx);
        await db.ExecuteAsync(@"
            INSERT RelayParticipants(ChainId,UserId,Position,ReceiveFinalOutcome,JoinedAt)
            VALUES(@ChainId,@UserId,0,0,@Now);
            INSERT RelayHandoffs(ChainId,Position,SenderUserId,TokenHash,TransferMethod,Status,CreatedAt,ExpiresAt)
            VALUES(@ChainId,1,@UserId,@Hash,@Method,'Pending',@Now,@ExpiresAt)",
            new { ChainId=chainId, UserId=userId, Hash=Hash(token), Method=request.TransferMethod, Now=utcNow, ExpiresAt=RelayRules.Deadline(utcNow,request.HandoffTtlMinutes) }, tx);
        tx.Commit();
        return new(chainId, token, RelayRules.Deadline(utcNow, request.HandoffTtlMinutes), utcNow);
    }

    public async Task<RelayHandoffView?> GetHandoffAsync(string token, long? userId, DateTime utcNow)
    {
        using var db=context.CreateConnection();
        var row=await db.QuerySingleOrDefaultAsync<HandoffRow>(@"
            SELECT h.ChainId,c.PollId,p.Question,h.Status,h.ExpiresAt,
              (SELECT COUNT(*) FROM RelayParticipants rp WHERE rp.ChainId=c.Id AND rp.VoteId IS NOT NULL) ChainLength,
              CASE WHEN h.Status='Pending' AND h.ExpiresAt>@Now THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END CanAccept,
              CASE WHEN h.ReceiverUserId=@UserId THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END IsAcceptedByCurrentUser
            FROM RelayHandoffs h JOIN RelayChains c ON c.Id=h.ChainId JOIN Polls p ON p.Id=c.PollId
            WHERE h.TokenHash=@Hash", new { Hash=Hash(token), UserId=userId, Now=utcNow });
        if(row is null) return null;
        var options=(await db.QueryAsync<PollOption>("SELECT Id,PollId,[Text] FROM PollOptions WHERE PollId=@PollId ORDER BY Id",new{row.PollId})).ToList();
        return new(){ChainId=row.ChainId,PollId=row.PollId,Question=row.Question,Options=options,Status=row.Status,ChainLength=row.ChainLength,NextMilestone=RelayRules.NextMilestone(row.ChainLength,DefaultMilestones),ExpiresAt=row.ExpiresAt,ServerNow=utcNow,CanAccept=row.CanAccept,IsAcceptedByCurrentUser=row.IsAcceptedByCurrentUser};
    }

    public async Task AcceptAsync(string token,long userId,DateTime utcNow)
    {
        using var db=context.CreateConnection();db.Open();using var tx=db.BeginTransaction(IsolationLevel.Serializable);
        var h=await db.QuerySingleOrDefaultAsync<HandoffLock>(@"SELECT h.Id,h.ChainId,h.Position,h.SenderUserId,h.ReceiverUserId,h.Status,h.ExpiresAt
          FROM RelayHandoffs h WITH(UPDLOCK,HOLDLOCK) JOIN RelayChains c WITH(UPDLOCK,HOLDLOCK) ON c.Id=h.ChainId WHERE h.TokenHash=@Hash",new{Hash=Hash(token)},tx);
        if(h is null) throw new RelayDomainException(RelayErrorCodes.Replayed,"Handoff is invalid or has already been used.");
        if(h.ExpiresAt<=utcNow) throw new RelayDomainException(RelayErrorCodes.Expired,"This handoff has expired.");
        if(h.Status=="Accepted" && h.ReceiverUserId==userId){tx.Commit();return;}
        if(h.Status!="Pending") throw new RelayDomainException(RelayErrorCodes.Replayed,"This handoff has already been accepted.");
        RelayRules.EnsureDifferentUsers(h.SenderUserId,userId);
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM RelayParticipants WHERE ChainId=@ChainId AND UserId=@UserId",new{h.ChainId,UserId=userId},tx)>0)
            throw new RelayDomainException(RelayErrorCodes.CycleDetected,"This participant already belongs to the chain.");
        if(await db.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM UserBlocks WHERE (BlockerUserId=@Sender AND BlockedUserId=@Receiver) OR (BlockerUserId=@Receiver AND BlockedUserId=@Sender)",new{Sender=h.SenderUserId,Receiver=userId},tx)>0)
            throw new RelayDomainException(RelayErrorCodes.Blocked,"This transfer is not available.");
        await db.ExecuteAsync(@"UPDATE RelayHandoffs SET ReceiverUserId=@UserId,Status='Accepted',AcceptedAt=@Now WHERE Id=@Id;
          INSERT RelayParticipants(ChainId,UserId,Position,AcceptedHandoffId,ReceiveFinalOutcome,JoinedAt)
          VALUES(@ChainId,@UserId,@Position,@Id,0,@Now)",new{UserId=userId,Now=utcNow,h.Id,h.ChainId,h.Position},tx);
        tx.Commit();
    }

    public async Task<RelayCompleteResult> CompleteAsync(string token,long userId,CompleteRelayRequest request,DateTime utcNow)
    {
        ValidateMethod(request.EndChain?"Link":request.NextTransferMethod);
        var nextToken=request.EndChain?null:NewToken();
        using var db=context.CreateConnection();db.Open();using var tx=db.BeginTransaction(IsolationLevel.Serializable);
        var h=await db.QuerySingleOrDefaultAsync<CompleteLock>(@"SELECT h.Id,h.ChainId,h.Position,h.ReceiverUserId,h.Status,h.ExpiresAt,c.PollId,c.MaxLength,c.HandoffTtlMinutes,c.Status ChainStatus
          FROM RelayHandoffs h WITH(UPDLOCK,HOLDLOCK) JOIN RelayChains c WITH(UPDLOCK,HOLDLOCK) ON c.Id=h.ChainId WHERE h.TokenHash=@Hash",new{Hash=Hash(token)},tx);
        if(h is null) throw new RelayDomainException(RelayErrorCodes.Replayed,"Handoff is invalid.");
        if(h.Status=="Completed" && h.ReceiverUserId==userId)
        { var existing=await ExistingResult(db,tx,h.ChainId,userId,utcNow);tx.Commit();return existing; }
        if(h.ExpiresAt<=utcNow) throw new RelayDomainException(RelayErrorCodes.Expired,"This handoff has expired.");
        if(h.Status!="Accepted"||h.ReceiverUserId!=userId) throw new RelayDomainException(RelayErrorCodes.Replayed,"Accept this handoff before completing it.");
        if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM PollOptions WHERE Id=@OptionId AND PollId=@PollId",new{request.OptionId,h.PollId},tx)!=1)
            throw new RelayDomainException(RelayErrorCodes.Invalid,"Choose a valid Up or Against option.");
        var voteId=await db.ExecuteScalarAsync<long>(@"INSERT Votes(PollId,OptionId,UserId,CreatedAt) OUTPUT inserted.Id VALUES(@PollId,@OptionId,@UserId,@Now)",new{h.PollId,request.OptionId,UserId=userId,Now=utcNow},tx);
        await db.ExecuteAsync(@"UPDATE PollOptions SET VoteCount=VoteCount+1 WHERE Id=@OptionId; UPDATE Polls SET TotalVotes=TotalVotes+1 WHERE Id=@PollId;
          UPDATE RelayParticipants SET VoteId=@VoteId,ReceiveFinalOutcome=@Consent,VotedAt=@Now WHERE ChainId=@ChainId AND UserId=@UserId;
          UPDATE RelayHandoffs SET Status='Completed',CompletedAt=@Now WHERE Id=@Id",new{request.OptionId,h.PollId,VoteId=voteId,Consent=request.ReceiveFinalOutcome,Now=utcNow,h.ChainId,UserId=userId,h.Id},tx);
        var length=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM RelayParticipants WHERE ChainId=@ChainId AND VoteId IS NOT NULL",new{h.ChainId},tx);
        var suspicious=await db.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM MobileDeviceTokens a JOIN MobileDeviceTokens b ON a.DeviceId=b.DeviceId AND a.DeviceId IS NOT NULL AND a.DeviceId<>'' WHERE a.UserId=@Sender AND b.UserId=@Receiver AND a.IsActive=1 AND b.IsActive=1",new{Sender=h.SenderUserId,Receiver=userId},tx)>0;
        if(suspicious) await db.ExecuteAsync(@"IF NOT EXISTS(SELECT 1 FROM RelayAbuseSignals WHERE ChainId=@ChainId AND ActorUserId=@Receiver AND RelatedUserId=@Sender AND SignalType='SharedDevice') INSERT RelayAbuseSignals(ChainId,ActorUserId,RelatedUserId,SignalType,Severity,Details,DetectedAt,RewardsSuppressed) VALUES(@ChainId,@Receiver,@Sender,'SharedDevice',3,'High-confidence shared device signal',@Now,1)",new{h.ChainId,Receiver=userId,Sender=h.SenderUserId,Now=utcNow},tx);
        await db.ExecuteAsync(@"
          DECLARE @MilestoneId bigint=(SELECT Id FROM RelayMilestones WHERE Length=@Length AND IsEnabled=1 AND @Eligible=1);
          IF @MilestoneId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM RelayMilestoneAwards WITH(UPDLOCK,HOLDLOCK) WHERE ChainId=@ChainId AND MilestoneId=@MilestoneId AND UserId=@UserId)
          BEGIN
            INSERT RelayMilestoneAwards(ChainId,MilestoneId,UserId,CreatedAt) VALUES(@ChainId,@MilestoneId,@UserId,@Now);
            DECLARE @BadgeId bigint=(SELECT b.Id FROM AchievementBadges b JOIN RelayMilestones m ON m.BadgeCode=b.Code WHERE m.Id=@MilestoneId);
            IF @BadgeId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM UserBadges WHERE UserId=@UserId AND BadgeId=@BadgeId)
              INSERT UserBadges(UserId,BadgeId,AwardedAt) VALUES(@UserId,@BadgeId,@Now);
            UPDATE RelayMilestoneAwards SET BadgeDeliveredAt=@Now WHERE ChainId=@ChainId AND MilestoneId=@MilestoneId AND UserId=@UserId;
          END",new{Length=length,h.ChainId,UserId=userId,Now=utcNow,Eligible=!suspicious},tx);
        var terminal=RelayRules.IsTerminal(length,h.MaxLength,request.EndChain);
        DateTime? expires=null;
        if(terminal)
            await db.ExecuteAsync(@"UPDATE RelayChains SET Status='Completed',CompletedAt=@Now,FinalizedAt=@Now WHERE Id=@ChainId;
              INSERT Notifications(UserId,Type,Title,Body,PollId,DedupKey,IsRead,CreatedAt)
              SELECT rp.UserId,'RelayOutcome','Your Relay outcome is ready','The final aggregate result is available.',c.PollId,CONCAT('relay-outcome:',c.Id,':',rp.UserId),0,@Now
              FROM RelayParticipants rp JOIN RelayChains c ON c.Id=rp.ChainId WHERE rp.ChainId=@ChainId AND rp.ReceiveFinalOutcome=1
              AND NOT EXISTS(SELECT 1 FROM Notifications n WHERE n.DedupKey=CONCAT('relay-outcome:',c.Id,':',rp.UserId))",new{Now=utcNow,h.ChainId},tx);
        else
        {
            if(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM RelayHandoffs WHERE ChainId=@ChainId AND Position=@Position",new{h.ChainId,Position=h.Position+1},tx)>0)
                throw new RelayDomainException(RelayErrorCodes.BranchConflict,"The chain already has a successor.");
            expires=utcNow.AddMinutes(h.HandoffTtlMinutes);
            await db.ExecuteAsync(@"INSERT RelayHandoffs(ChainId,Position,SenderUserId,TokenHash,TransferMethod,Status,CreatedAt,ExpiresAt)
              VALUES(@ChainId,@Position,@UserId,@Hash,@Method,'Pending',@Now,@Expires)",new{h.ChainId,Position=h.Position+1,UserId=userId,Hash=Hash(nextToken!),Method=request.NextTransferMethod,Now=utcNow,Expires=expires},tx);
        }
        tx.Commit();
        return new(h.ChainId,terminal?"Completed":"Active",length,RelayRules.NextMilestone(length,DefaultMilestones),nextToken,expires,false,!suspicious,utcNow);
    }

    public async Task<RelayProgress?> GetProgressAsync(long chainId,long userId,DateTime utcNow)
    { using var db=context.CreateConnection();var row=await db.QuerySingleOrDefaultAsync<ProgressRow>(@"SELECT c.Id ChainId,c.PollId,c.Status,c.MaxLength,rp.ReceiveFinalOutcome,
        (SELECT COUNT(*) FROM RelayParticipants x WHERE x.ChainId=c.Id AND x.VoteId IS NOT NULL) ChainLength,
        (SELECT MAX(ExpiresAt) FROM RelayHandoffs h WHERE h.ChainId=c.Id AND h.Status IN('Pending','Accepted')) CurrentDeadline
        FROM RelayChains c JOIN RelayParticipants rp ON rp.ChainId=c.Id AND rp.UserId=@UserId WHERE c.Id=@ChainId",new{ChainId=chainId,UserId=userId});
      return row is null?null:new(){ChainId=row.ChainId,PollId=row.PollId,Status=row.Status,MaxLength=row.MaxLength,ChainLength=row.ChainLength,NextMilestone=RelayRules.NextMilestone(row.ChainLength,DefaultMilestones),CurrentDeadline=row.CurrentDeadline,ServerNow=utcNow,ReceiveFinalOutcome=row.ReceiveFinalOutcome}; }
    public async Task SetConsentAsync(long chainId,long userId,bool receive){using var db=context.CreateConnection();if(await db.ExecuteAsync("UPDATE RelayParticipants SET ReceiveFinalOutcome=@Receive WHERE ChainId=@ChainId AND UserId=@UserId",new{Receive=receive,ChainId=chainId,UserId=userId})==0)throw new RelayDomainException(RelayErrorCodes.Forbidden,"You are not a chain member.");}
    public async Task<RelayOutcome?> GetOutcomeAsync(long chainId,long userId)
    {using var db=context.CreateConnection();var info=await db.QuerySingleOrDefaultAsync<OutcomeRow>(@"SELECT c.PollId,c.FinalizedAt FROM RelayChains c JOIN RelayParticipants rp ON rp.ChainId=c.Id AND rp.UserId=@UserId AND rp.ReceiveFinalOutcome=1 WHERE c.Id=@ChainId AND c.Status IN('Completed','Expired') AND c.FinalizedAt IS NOT NULL",new{ChainId=chainId,UserId=userId});if(info is null) return null;var opts=(await db.QueryAsync<RelayOutcomeOption>(@"SELECT Id OptionId,[Text],VoteCount,CAST(CASE WHEN p.TotalVotes=0 THEN 0 ELSE VoteCount*100.0/p.TotalVotes END AS float) VotePercentage FROM PollOptions o JOIN Polls p ON p.Id=o.PollId WHERE o.PollId=@PollId",new{info.PollId})).ToList();return new(chainId,opts.Sum(x=>x.VoteCount),opts,info.FinalizedAt!.Value);}
    public async Task<int> ExpireOverdueAsync(DateTime utcNow){using var db=context.CreateConnection();db.Open();using var tx=db.BeginTransaction(IsolationLevel.Serializable);var ids=(await db.QueryAsync<long>("SELECT Id FROM RelayHandoffs WITH(UPDLOCK,HOLDLOCK) WHERE Status IN('Pending','Accepted') AND ExpiresAt<=@Now",new{Now=utcNow},tx)).ToArray();if(ids.Length==0){tx.Commit();return 0;}await db.ExecuteAsync(@"UPDATE RelayHandoffs SET Status='Expired' WHERE Id IN @Ids; UPDATE RelayChains SET Status='Expired',ExpiredAt=@Now,FinalizedAt=COALESCE(FinalizedAt,@Now) WHERE Id IN(SELECT ChainId FROM RelayHandoffs WHERE Id IN @Ids) AND Status='Active';
      INSERT Notifications(UserId,Type,Title,Body,PollId,DedupKey,IsRead,CreatedAt)
      SELECT rp.UserId,'RelayOutcome','Your Relay outcome is ready','The final aggregate result is available.',c.PollId,CONCAT('relay-outcome:',c.Id,':',rp.UserId),0,@Now FROM RelayParticipants rp JOIN RelayChains c ON c.Id=rp.ChainId WHERE c.Id IN(SELECT ChainId FROM RelayHandoffs WHERE Id IN @Ids) AND rp.ReceiveFinalOutcome=1 AND NOT EXISTS(SELECT 1 FROM Notifications n WHERE n.DedupKey=CONCAT('relay-outcome:',c.Id,':',rp.UserId))",new{Ids=ids,Now=utcNow},tx);tx.Commit();return ids.Length;}
    private static async Task<RelayCompleteResult> ExistingResult(IDbConnection db,IDbTransaction tx,long chainId,long userId,DateTime now){var r=await db.QuerySingleAsync<ExistingRow>(@"SELECT c.Status,(SELECT COUNT(*) FROM RelayParticipants WHERE ChainId=c.Id AND VoteId IS NOT NULL) ChainLength FROM RelayChains c WHERE c.Id=@ChainId",new{ChainId=chainId},tx);var eligible=await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM RelayAbuseSignals WHERE ChainId=@ChainId AND ActorUserId=@UserId AND RewardsSuppressed=1",new{ChainId=chainId,UserId=userId},tx)==0;return new(chainId,r.Status,r.ChainLength,RelayRules.NextMilestone(r.ChainLength,DefaultMilestones),null,null,false,eligible,now);}
    private sealed class HandoffRow{public long ChainId{get;set;}public long PollId{get;set;}public string Question{get;set;}="";public string Status{get;set;}="";public DateTime ExpiresAt{get;set;}public int ChainLength{get;set;}public bool CanAccept{get;set;}public bool IsAcceptedByCurrentUser{get;set;}}
    private class HandoffLock{public long Id{get;set;}public long ChainId{get;set;}public int Position{get;set;}public long SenderUserId{get;set;}public long? ReceiverUserId{get;set;}public string Status{get;set;}="";public DateTime ExpiresAt{get;set;}}
    private sealed class CompleteLock:HandoffLock{public long PollId{get;set;}public int MaxLength{get;set;}public int HandoffTtlMinutes{get;set;}public string ChainStatus{get;set;}="";}
    private sealed class ProgressRow{public long ChainId{get;set;}public long PollId{get;set;}public string Status{get;set;}="";public int MaxLength{get;set;}public int ChainLength{get;set;}public DateTime? CurrentDeadline{get;set;}public bool ReceiveFinalOutcome{get;set;}}
    private sealed class OutcomeRow{public long PollId{get;set;}public DateTime? FinalizedAt{get;set;}}
    private sealed class ExistingRow{public string Status{get;set;}="";public int ChainLength{get;set;}}
}
