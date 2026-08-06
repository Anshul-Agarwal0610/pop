using System.Data;
using System.Text.Json;
using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BackendAPI.Repository;

public sealed class GameSessionsRepository(
    DapperContext context,
    IChallengesRepository challenges,
    IAchievementsRepository achievements,
    IUsersRepository users) : IGameSessionsRepository
{
    private const int PollCount = 5;
    private const int CompletionXp = 100;
    private const int TimeLimitSeconds = 120;

    public async Task<GameSessionDto?> GetActiveAsync(long userId, DateTime utcNow)
    {
        using var conn = context.CreateConnection();
        var id = await conn.QueryFirstOrDefaultAsync<long?>(
            "SELECT TOP 1 Id FROM GameSessions WHERE UserId=@UserId AND Status='Active' ORDER BY StartedAt DESC", new { UserId = userId });
        return id is null ? null : await GetAsync(id.Value, userId, utcNow);
    }

    public async Task<GameSessionDto?> GetAsync(long id, long userId, DateTime utcNow)
    {
        using var conn = context.CreateConnection();
        conn.Open();
        var session = await conn.QueryFirstOrDefaultAsync<GameSessionDto>(
            "SELECT Id,Mode,Category,Status,PollCount,CurrentPosition,VotesCast,TimeLimitSeconds,CompletionXp,StartedAt,ExpiresAt,CompletedAt FROM GameSessions WHERE Id=@Id AND UserId=@UserId",
            new { Id = id, UserId = userId });
        if (session is null) return null;

        if (session.Status == GameSessionStatuses.Active && GameSessionRules.IsExpired(session.ExpiresAt, utcNow))
        {
            await conn.ExecuteAsync("UPDATE GameSessions SET Status='Expired',UpdatedAt=@Now WHERE Id=@Id AND Status='Active'", new { Id = id, Now = utcNow });
            session.Status = GameSessionStatuses.Expired;
        }
        session.ServerNow = utcNow;
        if (session.Status == GameSessionStatuses.Active)
            session.CurrentPoll = await LoadCurrentPoll(conn, id, session.CurrentPosition);
        if (session.Status == GameSessionStatuses.Completed)
            session.Summary = await LoadSummary(conn, id);
        return session;
    }

    public async Task<GameSessionDto> StartOrResumeAsync(long userId, StartGameSessionRequest request, DateTime utcNow)
    {
        if (!request.Mode.Equals(GameModes.OpinionSprint, StringComparison.OrdinalIgnoreCase))
            throw new GameSessionException("unavailable", "That game mode is unavailable.");
        var existing = await GetActiveAsync(userId, utcNow);
        if (existing is not null) return existing;

        using var conn = context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var category = CategoryCatalog.NormalizeName(request.Category);
        var pollIds = (await conn.QueryAsync<long>(@"
            SELECT TOP (@Count) p.Id FROM Polls p WITH (UPDLOCK, HOLDLOCK)
            WHERE p.IsActive=1 AND p.ExpiresAt>@Now AND p.ModerationStatus='Published'
              AND COALESCE(p.IsPrivate,0)=0 AND COALESCE(p.IsWellness,0)=0 AND COALESCE(p.IsSponsored,0)=0
              AND p.Category <> 'Health' AND LOWER(p.Category)=LOWER(@Category)
              AND NOT EXISTS (SELECT 1 FROM Votes v WHERE v.UserId=@UserId AND v.PollId=p.Id)
            ORDER BY NEWID()", new { Count = PollCount, Now = utcNow, Category = category, UserId = userId }, tx)).ToList();
        if (pollIds.Count != PollCount)
            throw new GameSessionException("insufficient_content", $"{category} does not have enough unvoted polls for this round.");

        var expiresAt = request.Timed ? utcNow.AddSeconds(TimeLimitSeconds) : (DateTime?)null;
        long id;
        try
        {
            id = await conn.ExecuteScalarAsync<long>(@"
                INSERT INTO GameSessions(UserId,Mode,Category,PollCount,TimeLimitSeconds,CompletionXp,Status,StartedAt,ExpiresAt,UpdatedAt)
                VALUES(@UserId,@Mode,@Category,@PollCount,@Limit,@CompletionXp,'Active',@Now,@ExpiresAt,@Now);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                new { UserId = userId, Mode = GameModes.OpinionSprint, Category = category, PollCount, Limit = request.Timed ? TimeLimitSeconds : (int?)null, CompletionXp, Now = utcNow, ExpiresAt = expiresAt }, tx);
            for (var position = 0; position < pollIds.Count; position++)
                await conn.ExecuteAsync("INSERT INTO GameSessionPolls(SessionId,PollId,Position) VALUES(@Id,@PollId,@Position)", new { Id = id, PollId = pollIds[position], Position = position }, tx);
            tx.Commit();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            tx.Rollback();
            return (await GetActiveAsync(userId, utcNow))!;
        }
        return (await GetAsync(id, userId, utcNow))!;
    }

    public async Task<GameVoteResult> VoteAsync(long id, long userId, GameVoteRequest request, DateTime utcNow)
    {
        using var conn = context.CreateConnection(); conn.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT * FROM GameSessions WITH (UPDLOCK,ROWLOCK) WHERE Id=@Id AND UserId=@UserId", new { Id = id, UserId = userId }, tx)
            ?? throw new GameSessionException("not_found", "Round not found.");
        if ((string)row.Status != GameSessionStatuses.Active) throw new GameSessionException(((string)row.Status).ToLowerInvariant(), "This round is no longer active.");
        if (GameSessionRules.IsExpired((DateTime?)row.ExpiresAt, utcNow))
        {
            await conn.ExecuteAsync("UPDATE GameSessions SET Status='Expired',UpdatedAt=@Now WHERE Id=@Id", new { Id = id, Now = utcNow }, tx); tx.Commit();
            throw new GameSessionException("expired", "Time is up for this round.");
        }
        if (!GameSessionRules.IsCurrentPosition((int)row.CurrentPosition, request.Position)) throw new GameSessionException("invalid_position", "Only the current poll can be answered.");
        var poll = await LoadCurrentPoll(conn, id, request.Position, tx);
        if (poll is null || poll.Id != request.PollId || !poll.IsActive || poll.ExpiresAt <= utcNow || poll.ModerationStatus != PollModerationStatus.Published)
        {
            await conn.ExecuteAsync("UPDATE GameSessions SET Status='Expired',UpdatedAt=@Now WHERE Id=@Id", new { Id = id, Now = utcNow }, tx); tx.Commit();
            throw new GameSessionException("poll_unavailable", "The current poll is no longer available.");
        }
        if (!poll.Options.Any(o => o.Id == request.OptionId)) throw new GameSessionException("invalid_option", "That option does not belong to the current poll.");
        var voteXp = GamificationRules.VoteXp(poll);
        long voteId;
        try
        {
            voteId = await conn.ExecuteScalarAsync<long>(@"INSERT INTO Votes(PollId,OptionId,UserId,CreatedAt)
                OUTPUT inserted.Id VALUES(@PollId,@OptionId,@UserId,@Now)", new { request.PollId, request.OptionId, UserId = userId, Now = utcNow }, tx);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { throw new GameSessionException("already_voted", "You have already voted on this poll."); }
        await conn.ExecuteAsync("UPDATE PollOptions SET VoteCount=VoteCount+1 WHERE Id=@OptionId AND PollId=@PollId; UPDATE Polls SET TotalVotes=TotalVotes+1 WHERE Id=@PollId", request, tx);
        await conn.ExecuteAsync("UPDATE GameSessionPolls SET VotedOptionId=@OptionId,VotedAt=@Now WHERE SessionId=@Id AND Position=@Position AND VotedOptionId IS NULL", new { Id = id, request.Position, request.OptionId, Now = utcNow }, tx);
        var isFinal = request.Position + 1 == (int)row.PollCount;
        var completionAward = isFinal ? (int)row.CompletionXp : 0;
        await conn.ExecuteAsync(@"UPDATE GameSessions SET CurrentPosition=CurrentPosition+1,VotesCast=VotesCast+1,VoteXpEarned=VoteXpEarned+@VoteXp,
              Status=CASE WHEN @Final=1 THEN 'Completed' ELSE Status END,CompletedAt=CASE WHEN @Final=1 THEN @Now ELSE CompletedAt END,
              CompletionXpAwarded=CASE WHEN @Final=1 AND RewardGrantedAt IS NULL THEN @CompletionXp ELSE CompletionXpAwarded END,
              RewardGrantedAt=CASE WHEN @Final=1 AND RewardGrantedAt IS NULL THEN @Now ELSE RewardGrantedAt END,UpdatedAt=@Now WHERE Id=@Id;
            UPDATE Users SET Xp=Xp+@CompletionXp WHERE Id=@UserId AND @Final=1;",
            new { Id = id, UserId = userId, Xp = voteXp, VoteXp = voteXp, Final = isFinal, CompletionXp = completionAward, Now = utcNow }, tx);
        tx.Commit();

        await users.ApplyVoteRewardAsync(userId, poll.Id, voteXp, utcNow, leaderboardEligible: true);
        var challengeProgress = await challenges.AdvanceForVoteAsync(userId, voteId, poll, utcNow);
        var unlocked = (await achievements.AwardEligibleBadgesAsync(userId, utcNow)).AwardedBadges;
        if (isFinal) await SaveSummary(id, userId, challengeProgress, unlocked);
        return new GameVoteResult { Session = (await GetAsync(id, userId, utcNow))!, XpAwarded = voteXp + completionAward, Challenges = challengeProgress, AchievementsUnlocked = unlocked };
    }

    public async Task<GameSessionDto> CompleteAsync(long id, long userId, DateTime utcNow)
    {
        var session = await GetAsync(id, userId, utcNow) ?? throw new GameSessionException("not_found", "Round not found.");
        if (session.Status == GameSessionStatuses.Active) throw new GameSessionException("incomplete", "Answer every poll before completing the round.");
        return session;
    }

    private async Task SaveSummary(long id, long userId, IEnumerable<UserChallenge> challengeProgress, IEnumerable<UserBadge> unlocked)
    {
        using var conn = context.CreateConnection();
        var values = await conn.QuerySingleAsync<(int VotesCast, int VoteXpEarned, int CompletionXpAwarded)>("SELECT VotesCast,VoteXpEarned,CompletionXpAwarded FROM GameSessions WHERE Id=@Id AND UserId=@UserId", new { Id = id, UserId = userId });
        var summary = new CompletionSummaryDto { Votes = values.VotesCast, VoteXpEarned = values.VoteXpEarned, CompletionXpEarned = values.CompletionXpAwarded, ChallengeProgress = challengeProgress, AchievementsUnlocked = unlocked };
        await conn.ExecuteAsync("UPDATE GameSessions SET CompletionSummary=COALESCE(CompletionSummary,@Json) WHERE Id=@Id", new { Id = id, Json = JsonSerializer.Serialize(summary) });
    }

    private static async Task<CompletionSummaryDto?> LoadSummary(IDbConnection conn, long id)
    {
        var json = await conn.QueryFirstOrDefaultAsync<string>("SELECT CompletionSummary FROM GameSessions WHERE Id=@Id", new { Id = id });
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<CompletionSummaryDto>(json);
    }

    private static async Task<Poll?> LoadCurrentPoll(IDbConnection conn, long id, int position, IDbTransaction? tx = null)
    {
        var dict = new Dictionary<long, Poll>();
        await conn.QueryAsync<Poll, PollOption, Poll>(@"SELECT p.*,o.* FROM GameSessionPolls sp JOIN Polls p ON p.Id=sp.PollId LEFT JOIN PollOptions o ON o.PollId=p.Id WHERE sp.SessionId=@Id AND sp.Position=@Position",
            (p,o) => { if (!dict.TryGetValue(p.Id,out var found)) { found=p; found.Options=[]; dict[p.Id]=found; } if (o is not null) found.Options.Add(o); return found; }, new { Id=id, Position=position }, tx, splitOn:"Id");
        return dict.Values.FirstOrDefault();
    }
}
