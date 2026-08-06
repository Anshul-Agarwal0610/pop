using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository;

public class ChallengesRepository : IChallengesRepository
{
    private readonly DapperContext _context;
    private readonly IAchievementsRepository _achievementsRepo;

    public ChallengesRepository(DapperContext context, IAchievementsRepository achievementsRepo)
    {
        _context = context;
        _achievementsRepo = achievementsRepo;
    }

    public async Task EnsureCurrentOccurrencesAsync(DateTime utcNow)
    {
        using var conn = _context.CreateConnection();
        var definitions = await conn.QueryAsync<ChallengeDefinition>(
            "SELECT * FROM ChallengeDefinitions WHERE IsEnabled = 1");

        foreach (var definition in definitions)
        {
            var window = ChallengeDomain.Window(definition.Recurrence, utcNow);
            try
            {
                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM Challenges WITH (UPDLOCK, HOLDLOCK)
                        WHERE DefinitionId = @Id AND StartAt = @StartAt AND EndAt = @EndAt)
                    INSERT INTO Challenges
                        (DefinitionId, Title, Description, ChallengeType, Recurrence, RequirementType,
                         RequirementText, Category, RequiredVotes, RewardXp, RewardBadge, RewardBadgeId,
                         AllowPrivateVotes, AllowWellnessVotes, StartAt, EndAt, IsActive, CreatedAt)
                    VALUES
                        (@Id, @Title, @Description, @ChallengeType, @Recurrence, @RequirementType,
                         @RequirementText, @Category, @TargetCount, @RewardXp, @RewardBadge, @RewardBadgeId,
                         @AllowPrivateVotes, @AllowWellnessVotes, @StartAt, @EndAt, 1, SYSUTCDATETIME())",
                    new { definition.Id, definition.Title, definition.Description, definition.ChallengeType,
                        definition.Recurrence, definition.RequirementType, definition.RequirementText,
                        definition.Category, definition.TargetCount, definition.RewardXp, definition.RewardBadge,
                        definition.RewardBadgeId, definition.AllowPrivateVotes, definition.AllowWellnessVotes,
                        window.StartAt, window.EndAt });
            }
            catch (Exception ex) when (ex.Message.Contains("UQ_Challenges_DefinitionWindow")) { }
        }
    }

    public Task<IEnumerable<UserChallenge>> GetActiveForUserAsync(long userId, DateTime utcNow) =>
        GetForUserAsync(userId, utcNow, "active");

    public async Task<IEnumerable<UserChallenge>> GetForUserAsync(long userId, DateTime utcNow, string state = "active")
    {
        await EnsureCurrentOccurrencesAsync(utcNow);
        using var conn = _context.CreateConnection();
        var rows = (await conn.QueryAsync<UserChallenge>(@"
            SELECT c.Id AS ChallengeId, c.*, COALESCE(uc.CurrentVotes, 0) AS CurrentVotes,
                   CAST(COALESCE(uc.IsCompleted, 0) AS bit) AS IsCompleted,
                   CAST(COALESCE(uc.RewardGranted, 0) AS bit) AS RewardGranted, uc.CompletedAt
            FROM Challenges c
            LEFT JOIN UserChallengeProgress uc ON uc.ChallengeId = c.Id AND uc.UserId = @UserId
            WHERE c.IsActive = 1
            ORDER BY c.EndAt DESC, c.Id DESC", new { UserId = userId })).ToList();

        foreach (var row in rows)
        {
            row.CurrentVotes = Math.Min(row.RequiredVotes, row.CurrentVotes);
            row.State = ChallengeDomain.State(row.IsCompleted, row.CurrentVotes, row.EndAt, utcNow);
            row.EligiblePollsUrl = row.Category == null ? "/polls" : $"/polls?category={Uri.EscapeDataString(row.Category)}";
        }
        return state.ToLowerInvariant() switch
        {
            "active" => rows.Where(x => x.State is ChallengeStates.Available or ChallengeStates.InProgress),
            "completed" => rows.Where(x => x.State == ChallengeStates.Completed),
            "expired" => rows.Where(x => x.State == ChallengeStates.Expired),
            "all" => rows,
            _ => throw new ArgumentException("state must be active, completed, expired, or all", nameof(state))
        };
    }

    public async Task<IEnumerable<UserChallenge>> AdvanceForVoteAsync(long userId, long voteId, Poll poll, DateTime utcNow)
    {
        await EnsureCurrentOccurrencesAsync(utcNow);
        using var conn = _context.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var affectedIds = new List<long>();
        try
        {
            var challenges = (await conn.QueryAsync<Challenge>(@"
                SELECT * FROM Challenges WITH (UPDLOCK)
                WHERE IsActive = 1 AND StartAt <= @UtcNow AND EndAt > @UtcNow",
                new { UtcNow = utcNow }, transaction)).Where(c => ChallengeDomain.IsEligible(c, poll, utcNow));

            foreach (var challenge in challenges)
            {
                var recorded = await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM ChallengeProgressEvents WITH (UPDLOCK, HOLDLOCK)
                        WHERE UserId=@UserId AND ChallengeId=@ChallengeId AND VoteId=@VoteId)
                    BEGIN
                        INSERT ChallengeProgressEvents(UserId, ChallengeId, VoteId, CreatedAt)
                        VALUES(@UserId, @ChallengeId, @VoteId, SYSUTCDATETIME());
                    END", new { UserId = userId, ChallengeId = challenge.Id, VoteId = voteId }, transaction);
                if (recorded == 0) continue;

                await conn.ExecuteAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM UserChallengeProgress WITH (UPDLOCK, HOLDLOCK)
                        WHERE UserId=@UserId AND ChallengeId=@ChallengeId)
                    INSERT UserChallengeProgress(UserId, ChallengeId, CurrentVotes, IsCompleted, RewardGranted, CreatedAt, UpdatedAt)
                    VALUES(@UserId, @ChallengeId, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

                    UPDATE UserChallengeProgress WITH (UPDLOCK)
                    SET CurrentVotes = CASE WHEN CurrentVotes < @Target THEN CurrentVotes + 1 ELSE @Target END,
                        IsCompleted = CASE WHEN CurrentVotes + 1 >= @Target THEN 1 ELSE IsCompleted END,
                        CompletedAt = CASE WHEN CompletedAt IS NULL AND CurrentVotes + 1 >= @Target THEN @UtcNow ELSE CompletedAt END,
                        UpdatedAt = @UtcNow
                    WHERE UserId=@UserId AND ChallengeId=@ChallengeId;",
                    new { UserId = userId, ChallengeId = challenge.Id, Target = challenge.RequiredVotes, UtcNow = utcNow }, transaction);

                var claimed = await conn.ExecuteAsync(@"
                    UPDATE UserChallengeProgress SET RewardGranted=1, UpdatedAt=@UtcNow
                    WHERE UserId=@UserId AND ChallengeId=@ChallengeId AND IsCompleted=1 AND RewardGranted=0",
                    new { UserId = userId, ChallengeId = challenge.Id, UtcNow = utcNow }, transaction);
                if (claimed == 1)
                {
                    await conn.ExecuteAsync("UPDATE Users SET Xp=Xp+@RewardXp WHERE Id=@UserId",
                        new { challenge.RewardXp, UserId = userId }, transaction);
                    if (challenge.RewardBadgeId.HasValue)
                        await conn.ExecuteAsync(@"IF NOT EXISTS (SELECT 1 FROM UserBadges WHERE UserId=@UserId AND BadgeId=@BadgeId)
                            INSERT UserBadges(UserId, BadgeId, AwardedAt) VALUES(@UserId, @BadgeId, @UtcNow)",
                            new { UserId = userId, BadgeId = challenge.RewardBadgeId, UtcNow = utcNow }, transaction);
                }
                affectedIds.Add(challenge.Id);
            }
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }

        if (affectedIds.Count == 0) return Array.Empty<UserChallenge>();
        await _achievementsRepo.AwardEligibleBadgesAsync(userId, utcNow);
        var all = await GetForUserAsync(userId, utcNow, "all");
        return all.Where(x => affectedIds.Contains(x.ChallengeId));
    }

    private sealed class ChallengeDefinition
    {
        public long Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ChallengeType { get; set; } = "Voting";
        public string Recurrence { get; set; } = ChallengeRecurrences.Daily;
        public string RequirementType { get; set; } = "VoteCount";
        public string RequirementText { get; set; } = "";
        public int TargetCount { get; set; }
        public string? Category { get; set; }
        public int RewardXp { get; set; }
        public string? RewardBadge { get; set; }
        public long? RewardBadgeId { get; set; }
        public bool AllowPrivateVotes { get; set; }
        public bool AllowWellnessVotes { get; set; }
    }
}
