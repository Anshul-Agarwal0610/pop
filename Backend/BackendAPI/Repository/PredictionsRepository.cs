using System.Data;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BackendAPI.Repository;

public sealed class PredictionsRepository(DapperContext context, IPollsRepository polls) : IPredictionsRepository
{
    public async Task<IReadOnlyList<PredictionOpportunity>> GetAvailableAsync(long userId, string predictionType, int count, DateTime utcNow)
    {
        predictionType = NormalizeType(predictionType);
        using var conn = context.CreateConnection();
        var ids = await conn.QueryAsync<long>(@"SELECT TOP (@Count) p.Id FROM Polls p
            WHERE p.IsActive=1 AND p.ExpiresAt>DATEADD(minute,10,@Now) AND p.ModerationStatus='Published'
              AND COALESCE(p.IsPrivate,0)=0 AND COALESCE(p.IsWellness,0)=0
              AND (SELECT COUNT(1) FROM PollOptions o WHERE o.PollId=p.Id)=2
              AND NOT EXISTS (SELECT 1 FROM PollPredictions x WHERE x.UserId=@UserId AND x.PollId=p.Id AND x.PredictionType=@PredictionType)
            ORDER BY p.IsTrending DESC,p.TotalVotes DESC,p.CreatedAt DESC", new { Count=Math.Clamp(count,1,30), UserId=userId, PredictionType=predictionType, Now=utcNow });
        var result = new List<PredictionOpportunity>();
        foreach (var id in ids)
            if (await polls.GetByIdAsync(id, userId) is { } poll) result.Add(new PredictionOpportunity { Poll = poll });
        return result;
    }

    public async Task<IReadOnlyList<PredictionEntry>> GetMineAsync(long userId, int count, DateTime utcNow)
    {
        using var conn = context.CreateConnection();
        return (await conn.QueryAsync<PredictionEntry>(@"SELECT TOP (@Count) x.Id,x.PollId,p.Question,p.Category,x.PredictedOptionId,
            po.Text PredictedOptionText,x.PredictionType,x.PredictedPercentage,x.ActualPercentage,x.AccuracyDifference,
            x.WinningOptionId,wo.Text WinningOptionText,x.Status,x.XpAwarded,x.CreatedAt,x.LocksAt,x.SettledAt,p.TotalVotes
            FROM PollPredictions x JOIN Polls p ON p.Id=x.PollId JOIN PollOptions po ON po.Id=x.PredictedOptionId
            LEFT JOIN PollOptions wo ON wo.Id=x.WinningOptionId WHERE x.UserId=@UserId ORDER BY x.CreatedAt DESC",
            new { Count=Math.Clamp(count,1,100), UserId=userId })).ToList();
    }

    public async Task<PredictionEntry> CreateAsync(long userId, CreatePredictionRequest request, DateTime utcNow)
    {
        request.PredictionType = NormalizeType(request.PredictionType);
        if (request.PredictionType == PredictionTypes.Split && (request.PredictedPercentage is < 50 or > 95 or null))
            throw new PredictionException("invalid_percentage", "Choose a winning percentage between 50 and 95.");
        if (request.PredictionType == PredictionTypes.Winner) request.PredictedPercentage = null;
        using var conn = context.CreateConnection(); conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var poll = await conn.QueryFirstOrDefaultAsync<(DateTime ExpiresAt,bool IsActive,string ModerationStatus,bool IsPrivate,bool IsWellness)>(
            "SELECT ExpiresAt,IsActive,ModerationStatus,COALESCE(IsPrivate,0) IsPrivate,COALESCE(IsWellness,0) IsWellness FROM Polls WITH (UPDLOCK,HOLDLOCK) WHERE Id=@PollId", request, tx);
        if (poll == default) throw new PredictionException("not_found", "Poll not found.");
        if (!poll.IsActive || poll.ExpiresAt <= utcNow.AddMinutes(10) || poll.ModerationStatus != PollModerationStatus.Published || poll.IsPrivate || poll.IsWellness)
            throw new PredictionException("locked", "Predictions are closed for this poll.");
        if (!await conn.ExecuteScalarAsync<bool>("SELECT CASE WHEN EXISTS(SELECT 1 FROM PollOptions WHERE Id=@PredictedOptionId AND PollId=@PollId) THEN 1 ELSE 0 END", request, tx))
            throw new PredictionException("invalid_option", "That option does not belong to this poll.");
        try
        {
            await conn.ExecuteAsync(@"INSERT INTO PollPredictions(UserId,PollId,PredictedOptionId,PredictionType,PredictedPercentage,Status,CreatedAt,LocksAt)
                VALUES(@UserId,@PollId,@PredictedOptionId,@PredictionType,@PredictedPercentage,'Pending',@Now,@LocksAt)", new { UserId=userId, request.PollId, request.PredictedOptionId, request.PredictionType, request.PredictedPercentage, Now=utcNow, LocksAt=poll.ExpiresAt }, tx);
            tx.Commit();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { throw new PredictionException("already_predicted", "You already predicted this poll."); }
        return (await GetMineAsync(userId, 100, utcNow)).First(x => x.PollId == request.PollId);
    }

    public async Task<IReadOnlyList<PredictionRewardCandidate>> SettleMineAsync(long userId, DateTime utcNow)
    {
        using var conn = context.CreateConnection(); conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var pending = (await conn.QueryAsync<(long Id,long PollId,long PredictedOptionId,string PredictionType,decimal? PredictedPercentage)>(@"SELECT x.Id,x.PollId,x.PredictedOptionId,x.PredictionType,x.PredictedPercentage FROM PollPredictions x
            JOIN Polls p ON p.Id=x.PollId WHERE x.UserId=@UserId AND x.Status='Pending' AND p.ExpiresAt<=@Now", new { UserId=userId, Now=utcNow }, tx)).ToList();
        foreach (var item in pending)
        {
            var winners = (await conn.QueryAsync<(long Id,int VoteCount)>("SELECT Id,VoteCount FROM PollOptions WHERE PollId=@PollId ORDER BY VoteCount DESC", item, tx)).ToList();
            var winning = winners.FirstOrDefault();
            var tied = winners.Count > 1 && winners[1].VoteCount == winning.VoteCount;
            var noContest = winners.Count == 0 || winners.Sum(x => x.VoteCount) == 0 || tied;
            var actual = noContest ? (decimal?)null : Math.Round(winners.First(x => x.Id == item.PredictedOptionId).VoteCount * 100m / winners.Sum(x => x.VoteCount), 1);
            var difference = actual.HasValue && item.PredictedPercentage.HasValue ? Math.Abs(actual.Value-item.PredictedPercentage.Value) : (decimal?)null;
            var status = noContest ? PredictionStatuses.NoContest
                : item.PredictionType == PredictionTypes.Split ? difference <= 10 ? PredictionStatuses.Correct : PredictionStatuses.Incorrect
                : item.PredictedOptionId == winning.Id ? PredictionStatuses.Correct : PredictionStatuses.Incorrect;
            await conn.ExecuteAsync("UPDATE PollPredictions SET Status=@Status,WinningOptionId=@Winner,ActualPercentage=@Actual,AccuracyDifference=@Difference,SettledAt=@Now WHERE Id=@Id AND Status='Pending'",
                new { item.Id, Status=status, Winner=noContest ? (long?)null : winning.Id, Actual=actual, Difference=difference, Now=utcNow }, tx);
        }
        tx.Commit();

        // Include any previously settled prediction whose reward was not recorded yet.
        // This lets a later request recover safely if settlement completed but reward
        // granting was interrupted.
        return (await conn.QueryAsync<PredictionRewardCandidate>(@"SELECT Id,CASE
                WHEN PredictionType='Winner' THEN 'prediction.correct'
                WHEN AccuracyDifference<=1 THEN 'prediction.split.exact'
                WHEN AccuracyDifference<=3 THEN 'prediction.split.close3'
                WHEN AccuracyDifference<=5 THEN 'prediction.split.close5'
                ELSE 'prediction.split.close10' END RuleCode
            FROM PollPredictions WHERE UserId=@UserId AND Status='Correct' AND XpAwarded=0",
            new { UserId=userId })).ToList();
    }

    public async Task MarkRewardedAsync(long predictionId, int xp)
    {
        using var conn = context.CreateConnection();
        await conn.ExecuteAsync("UPDATE PollPredictions SET XpAwarded=@Xp WHERE Id=@Id AND XpAwarded=0", new { Id=predictionId, Xp=xp });
    }

    private static string NormalizeType(string? value) => value?.Equals(PredictionTypes.Split, StringComparison.OrdinalIgnoreCase) == true
        ? PredictionTypes.Split : PredictionTypes.Winner;
}

public sealed class PredictionException(string code, string message) : Exception(message) { public string Code { get; } = code; }
