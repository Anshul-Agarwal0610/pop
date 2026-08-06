using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;
using BackendAPI.Analytics;

namespace BackendAPI.Repository
{
    public class ChallengesRepository : IChallengesRepository
    {
        private readonly DapperContext _context;
        private readonly IAchievementsRepository _achievementsRepo;
        private readonly IAnalyticsOutbox _analytics;

        public ChallengesRepository(DapperContext context, IAchievementsRepository achievementsRepo, IAnalyticsOutbox analytics)
        {
            _context = context;
            _achievementsRepo = achievementsRepo;
            _analytics = analytics;
        }

        public async Task EnsureDailyChallengeAsync(DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            var start = utcNow.Date;
            var end = start.AddDays(1);

            var exists = await conn.ExecuteScalarAsync<int>(
                @"SELECT COUNT(1)
                  FROM Challenges
                  WHERE StartAt = @StartAt
                    AND EndAt = @EndAt
                    AND Title = @Title",
                new { StartAt = start, EndAt = end, Title = "Daily Pulse" });

            if (exists > 0) return;

            await conn.ExecuteAsync(
                @"INSERT INTO Challenges
                    (Title, Category, RequiredVotes, RewardXp, RewardBadge, StartAt, EndAt, IsActive, CreatedAt)
                  VALUES
                    (@Title, NULL, 3, 75, @RewardBadge, @StartAt, @EndAt, 1, GETUTCDATE())",
                new
                {
                    Title = "Daily Pulse",
                    RewardBadge = "Daily Voter",
                    StartAt = start,
                    EndAt = end
                });
        }

        public async Task<IEnumerable<UserChallenge>> GetActiveForUserAsync(long userId, DateTime utcNow)
        {
            await EnsureDailyChallengeAsync(utcNow);

            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<UserChallenge>(
                @"SELECT
                      c.Id AS ChallengeId,
                      c.Title,
                      c.Category,
                      c.RequiredVotes,
                      c.RewardXp,
                      c.RewardBadge,
                      c.StartAt,
                      c.EndAt,
                      COALESCE(uc.CurrentVotes, 0) AS CurrentVotes,
                      CAST(COALESCE(uc.IsCompleted, 0) AS bit) AS IsCompleted,
                      CAST(COALESCE(uc.RewardGranted, 0) AS bit) AS RewardGranted,
                      uc.CompletedAt
                  FROM Challenges c
                  LEFT JOIN UserChallengeProgress uc
                    ON uc.ChallengeId = c.Id AND uc.UserId = @UserId
                  WHERE c.IsActive = 1
                    AND c.StartAt <= @UtcNow
                    AND c.EndAt > @UtcNow
                  ORDER BY c.EndAt ASC, c.Id ASC",
                new { UserId = userId, UtcNow = utcNow });
        }

        public async Task<IEnumerable<UserChallenge>> AdvanceForVoteAsync(long userId, Poll poll, DateTime utcNow)
        {
            await EnsureDailyChallengeAsync(utcNow);

            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var challenges = (await conn.QueryAsync<Challenge>(
                    @"SELECT *
                      FROM Challenges
                      WHERE IsActive = 1
                        AND StartAt <= @UtcNow
                        AND EndAt > @UtcNow
                        AND (Category IS NULL OR LOWER(Category) = LOWER(@Category))",
                    new { UtcNow = utcNow, poll.Category },
                    transaction)).ToList();

                foreach (var challenge in challenges)
                {
                    var previousProgress = await conn.ExecuteScalarAsync<int?>("SELECT CurrentVotes FROM UserChallengeProgress WHERE UserId=@UserId AND ChallengeId=@ChallengeId", new { UserId = userId, ChallengeId = challenge.Id }, transaction) ?? 0;
                    await conn.ExecuteAsync(
                        @"IF NOT EXISTS (
                              SELECT 1 FROM UserChallengeProgress
                              WHERE UserId = @UserId AND ChallengeId = @ChallengeId
                          )
                          BEGIN
                              INSERT INTO UserChallengeProgress
                                  (UserId, ChallengeId, CurrentVotes, IsCompleted, RewardGranted, CreatedAt, UpdatedAt)
                              VALUES
                                  (@UserId, @ChallengeId, 0, 0, 0, GETUTCDATE(), GETUTCDATE())
                          END",
                        new { UserId = userId, ChallengeId = challenge.Id },
                        transaction);

                    var progress = await conn.QuerySingleAsync<UserChallenge>(
                        @"UPDATE UserChallengeProgress
                          SET CurrentVotes = CASE
                                  WHEN IsCompleted = 1 THEN CurrentVotes
                                  ELSE CASE
                                      WHEN CurrentVotes + 1 > @RequiredVotes THEN @RequiredVotes
                                      ELSE CurrentVotes + 1
                                  END
                              END,
                              IsCompleted = CASE
                                  WHEN IsCompleted = 1 OR CurrentVotes + 1 >= @RequiredVotes THEN 1
                                  ELSE 0
                              END,
                              CompletedAt = CASE
                                  WHEN CompletedAt IS NULL AND CurrentVotes + 1 >= @RequiredVotes THEN GETUTCDATE()
                                  ELSE CompletedAt
                              END,
                              UpdatedAt = GETUTCDATE()
                          OUTPUT
                              inserted.ChallengeId,
                              @Title AS Title,
                              @Category AS Category,
                              @RequiredVotes AS RequiredVotes,
                              @RewardXp AS RewardXp,
                              @RewardBadge AS RewardBadge,
                              @StartAt AS StartAt,
                              @EndAt AS EndAt,
                              inserted.CurrentVotes,
                              inserted.IsCompleted,
                              inserted.RewardGranted,
                              inserted.CompletedAt
                          WHERE UserId = @UserId AND ChallengeId = @ChallengeId",
                        new
                        {
                            UserId = userId,
                            ChallengeId = challenge.Id,
                            challenge.Title,
                            challenge.Category,
                            challenge.RequiredVotes,
                            challenge.RewardXp,
                            challenge.RewardBadge,
                            challenge.StartAt,
                            challenge.EndAt
                        },
                        transaction);

                    if (progress.IsCompleted && !progress.RewardGranted)
                    {
                        await conn.ExecuteAsync(
                            "UPDATE Users SET Xp = Xp + @RewardXp WHERE Id = @UserId",
                            new { UserId = userId, challenge.RewardXp },
                            transaction);

                        await conn.ExecuteAsync(
                            @"UPDATE UserChallengeProgress
                              SET RewardGranted = 1, UpdatedAt = GETUTCDATE()
                              WHERE UserId = @UserId AND ChallengeId = @ChallengeId",
                            new { UserId = userId, ChallengeId = challenge.Id },
                            transaction);
                    }
                    var consent = await conn.ExecuteScalarAsync<string>("SELECT AnalyticsConsent FROM Users WHERE Id=@UserId", new { UserId = userId }, transaction);
                    if (consent == "granted" && progress.CurrentVotes > previousProgress)
                    {
                        var eventName = previousProgress == 0 ? AnalyticsEventNames.ChallengeStarted : AnalyticsEventNames.ChallengeProgressed;
                        await _analytics.EnqueueAsync(conn, transaction, new AnalyticsEvent(Guid.NewGuid(), eventName, $"usr_{userId}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["challenge_id"] = challenge.Id.ToString(), [previousProgress == 0 ? "challenge_type" : "progress"] = previousProgress == 0 ? (object)(challenge.Category ?? "general") : progress.CurrentVotes, ["required_actions"] = challenge.RequiredVotes }, "challenge_id", previousProgress == 0 ? "challenge_type" : "progress", "required_actions"), utcNow, $"challenge:{userId}:{challenge.Id}:progress:{progress.CurrentVotes}"));
                        if (progress.IsCompleted) await _analytics.EnqueueAsync(conn, transaction, new AnalyticsEvent(Guid.NewGuid(), AnalyticsEventNames.ChallengeCompleted, $"usr_{userId}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["challenge_id"] = challenge.Id.ToString(), ["reward_xp"] = challenge.RewardXp, ["badge_granted"] = challenge.RewardBadge != null }, "challenge_id", "reward_xp", "badge_granted"), utcNow, $"challenge:{userId}:{challenge.Id}:completed"));
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            await _achievementsRepo.AwardEligibleBadgesAsync(userId, utcNow);

            return await GetActiveForUserAsync(userId, utcNow);
        }
    }
}
