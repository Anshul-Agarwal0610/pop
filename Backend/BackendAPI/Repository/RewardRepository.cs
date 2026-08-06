using System.Data;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BackendAPI.Repository;

public sealed class RewardRepository : IRewardRepository
{
    private readonly DapperContext _context;
    public RewardRepository(DapperContext context) => _context = context;

    public async Task<RewardGrantResult> GrantAsync(RewardGrantRequest request, CancellationToken cancellationToken = default)
    {
        var sourceKey = $"{request.SourceType.Trim().ToLowerInvariant()}:{request.SourceReference.Trim().ToLowerInvariant()}";
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var rule = await connection.QuerySingleOrDefaultAsync<RewardRule>(new CommandDefinition(
                @"SELECT TOP (1) * FROM RewardRules WITH (UPDLOCK, HOLDLOCK)
                  WHERE Code=@RuleCode AND IsEnabled=1 AND EffectiveFrom<=@At
                    AND (EffectiveTo IS NULL OR EffectiveTo>@At)
                  ORDER BY Version DESC", new { request.RuleCode, At = request.OccurredAtUtc }, transaction, cancellationToken: cancellationToken));
            if (rule is null) throw new InvalidOperationException($"No active reward rule exists for '{request.RuleCode}'.");

            var existing = await connection.QuerySingleOrDefaultAsync<RewardEvent>(new CommandDefinition(
                "SELECT * FROM RewardEvents WHERE UserId=@UserId AND SourceKey=@SourceKey",
                new { request.UserId, SourceKey = sourceKey }, transaction, cancellationToken: cancellationToken));
            if (existing is not null)
            {
                var total = await GetTotalAsync(connection, transaction, request.UserId, cancellationToken);
                transaction.Commit();
                return new(existing, total, true);
            }

            if (rule.PeriodLimit is int limit)
            {
                var periodStart = GetPeriodStart(request.OccurredAtUtc, rule.PeriodUnit, rule.PeriodValue ?? 1);
                var awarded = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    @"SELECT COALESCE(SUM(Value),0) FROM RewardEvents WITH (UPDLOCK, HOLDLOCK)
                      WHERE UserId=@UserId AND RuleCode=@RuleCode AND EventType='Grant' AND CreatedAt>=@PeriodStart",
                    new { request.UserId, request.RuleCode, PeriodStart = periodStart }, transaction, cancellationToken: cancellationToken));
                if (awarded + rule.Value > limit) throw new RewardLimitExceededException(rule.Code, limit);
            }

            var id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                @"INSERT RewardEvents(UserId,RuleId,RuleCode,RuleVersion,Reason,SourceType,SourceReference,SourceKey,Value,EventType,CreatedAt)
                  VALUES(@UserId,@RuleId,@RuleCode,@RuleVersion,@Reason,@SourceType,@SourceReference,@SourceKey,@Value,'Grant',@CreatedAt);
                  SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                new { request.UserId, RuleId=rule.Id, RuleCode=rule.Code, RuleVersion=rule.Version, rule.Reason,
                    request.SourceType, request.SourceReference, SourceKey=sourceKey, rule.Value, CreatedAt=request.OccurredAtUtc }, transaction, cancellationToken: cancellationToken));
            var currentXp = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "UPDATE Users SET Xp=Xp+@Value OUTPUT inserted.Xp WHERE Id=@UserId",
                new { request.UserId, rule.Value }, transaction, cancellationToken: cancellationToken));
            var rewardEvent = await connection.QuerySingleAsync<RewardEvent>(new CommandDefinition(
                "SELECT * FROM RewardEvents WHERE Id=@Id", new { Id=id }, transaction, cancellationToken: cancellationToken));
            transaction.Commit();
            return new(rewardEvent, currentXp, false);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            transaction.Rollback();
            var existing = await connection.QuerySingleAsync<RewardEvent>(new CommandDefinition(
                "SELECT * FROM RewardEvents WHERE UserId=@UserId AND SourceKey=@SourceKey", new { request.UserId, SourceKey=sourceKey }, cancellationToken:cancellationToken));
            return new(existing, await GetTotalAsync(connection, null, request.UserId, cancellationToken), true);
        }
        catch { transaction.Rollback(); throw; }
    }

    public async Task<RewardEvent> ReverseAsync(long eventId, long actorUserId, string reason, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        using var connection=_context.CreateConnection(); connection.Open(); using var tx=connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var original=await connection.QuerySingleOrDefaultAsync<RewardEvent>("SELECT * FROM RewardEvents WITH (UPDLOCK,HOLDLOCK) WHERE Id=@eventId",new{eventId},tx)
                ?? throw new KeyNotFoundException("Reward event not found.");
            if(original.EventType!="Grant" || original.Value<=0) throw new InvalidOperationException("Only positive grant events can be reversed.");
            var id=await connection.ExecuteScalarAsync<long>(@"INSERT RewardEvents(UserId,RuleId,RuleCode,RuleVersion,Reason,SourceType,SourceReference,SourceKey,Value,EventType,ReversesEventId,ActorUserId,CreatedAt)
                VALUES(@UserId,@RuleId,@RuleCode,@RuleVersion,@reason,'admin.reversal',@SourceReference,@SourceKey,@Value,'Reversal',@EventId,@actorUserId,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                new{original.UserId,original.RuleId,original.RuleCode,original.RuleVersion,reason,SourceReference=eventId.ToString(),SourceKey=$"reversal:{idempotencyKey}",Value=-original.Value,EventId=eventId,actorUserId},tx);
            await connection.ExecuteAsync("UPDATE Users SET Xp=Xp-@Value WHERE Id=@UserId",new{Value=original.Value,original.UserId},tx);
            var result=await connection.QuerySingleAsync<RewardEvent>("SELECT * FROM RewardEvents WHERE Id=@id",new{id},tx); tx.Commit(); return result;
        } catch {tx.Rollback();throw;}
    }

    public async Task<RewardEvent> AdjustAsync(long userId,int value,long actorUserId,string reason,string idempotencyKey,CancellationToken cancellationToken=default)
    {
        using var connection=_context.CreateConnection(); connection.Open(); using var tx=connection.BeginTransaction();
        try { var id=await connection.ExecuteScalarAsync<long>(@"INSERT RewardEvents(UserId,RuleCode,RuleVersion,Reason,SourceType,SourceReference,SourceKey,Value,EventType,ActorUserId,CreatedAt)
            VALUES(@userId,'admin.manual',1,@reason,'admin.adjustment',@idempotencyKey,@SourceKey,@value,'Adjustment',@actorUserId,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            new{userId,value,actorUserId,reason,idempotencyKey,SourceKey=$"adjustment:{idempotencyKey}"},tx); await connection.ExecuteAsync("UPDATE Users SET Xp=Xp+@value WHERE Id=@userId",new{userId,value},tx);
            var result=await connection.QuerySingleAsync<RewardEvent>("SELECT * FROM RewardEvents WHERE Id=@id",new{id},tx);tx.Commit();return result;} catch{tx.Rollback();throw;}
    }

    public async Task<IEnumerable<RewardEvent>> GetEventsAsync(long? userId,int count,CancellationToken cancellationToken=default)
    { using var c=_context.CreateConnection(); return await c.QueryAsync<RewardEvent>(new CommandDefinition("SELECT TOP (@Count) * FROM RewardEvents WHERE (@UserId IS NULL OR UserId=@UserId) ORDER BY CreatedAt DESC,Id DESC",new{UserId=userId,Count=Math.Clamp(count,1,200)},cancellationToken:cancellationToken)); }
    public async Task<IEnumerable<RewardRule>> GetActiveRulesAsync(DateTime utcNow,CancellationToken cancellationToken=default)
    { using var c=_context.CreateConnection(); return await c.QueryAsync<RewardRule>(new CommandDefinition("SELECT * FROM RewardRules WHERE IsEnabled=1 AND EffectiveFrom<=@utcNow AND (EffectiveTo IS NULL OR EffectiveTo>@utcNow) ORDER BY Code,Version DESC",new{utcNow},cancellationToken:cancellationToken)); }
    public async Task<IEnumerable<RewardReconciliation>> GetReconciliationAsync(CancellationToken cancellationToken=default)
    { using var c=_context.CreateConnection(); return await c.QueryAsync<RewardReconciliation>(new CommandDefinition(@"SELECT u.Id UserId,u.Xp CachedXp,CAST(COALESCE(SUM(e.Value),0) AS int) LedgerXp FROM Users u LEFT JOIN RewardEvents e ON e.UserId=u.Id GROUP BY u.Id,u.Xp HAVING u.Xp<>COALESCE(SUM(e.Value),0)",cancellationToken:cancellationToken)); }
    public async Task<IEnumerable<SuspiciousRewardActivity>> GetSuspiciousAsync(DateTime sinceUtc,int minimumEvents,CancellationToken cancellationToken=default)
    { using var c=_context.CreateConnection(); return await c.QueryAsync<SuspiciousRewardActivity>(new CommandDefinition(@"SELECT UserId,COUNT(*) EventCount,SUM(Value) NetXp,@sinceUtc WindowStart,GETUTCDATE() WindowEnd FROM RewardEvents WHERE CreatedAt>=@sinceUtc GROUP BY UserId HAVING COUNT(*)>=@minimumEvents ORDER BY EventCount DESC",new{sinceUtc,minimumEvents},cancellationToken:cancellationToken)); }

    internal static DateTime GetPeriodStart(DateTime utcNow,string? unit,int value) => (unit?.ToLowerInvariant()) switch
    { "hour" => new(utcNow.Year,utcNow.Month,utcNow.Day,utcNow.Hour,0,0,DateTimeKind.Utc), "week" => utcNow.Date.AddDays(-((7+(int)utcNow.DayOfWeek-(int)DayOfWeek.Monday)%7)), "month" => new(utcNow.Year,utcNow.Month,1,0,0,0,DateTimeKind.Utc), _ => utcNow.Date };
    private static Task<int> GetTotalAsync(IDbConnection c,IDbTransaction? tx,long userId,CancellationToken token) => c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT CAST(COALESCE(SUM(Value),0) AS int) FROM RewardEvents WHERE UserId=@userId",new{userId},tx,cancellationToken:token));
}

public sealed class RewardLimitExceededException : InvalidOperationException
{ public RewardLimitExceededException(string ruleCode,int limit):base($"Reward limit {limit} reached for '{ruleCode}'."){} }
