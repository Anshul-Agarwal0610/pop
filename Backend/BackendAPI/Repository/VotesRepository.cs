using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using BackendAPI.Analytics;
using Dapper;

namespace BackendAPI.Repository
{
    public class VotesRepository : IVotesRepository
    {
        private readonly DapperContext _context;
        private readonly IAchievementsRepository _achievementsRepo;
        private readonly IAnalyticsOutbox _analytics;

        public VotesRepository(DapperContext context, IAchievementsRepository achievementsRepo, IAnalyticsOutbox analytics)
        {
            _context = context;
            _achievementsRepo = achievementsRepo;
            _analytics = analytics;
        }

        public async Task<(long VoteId, VoteRewardResult Reward)> CastVoteAsync(
            CastVoteRequest request, long userId, int xpAwarded, DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var committed = false;
            try
            {
                // Serialize streak transitions for a user, including votes on different polls.
                var user = await conn.QuerySingleAsync<User>(
                    @"SELECT Id, Xp, Streak, LongestStreak, TotalVotes, LastVoteDate
                      FROM Users WITH (UPDLOCK, ROWLOCK) WHERE Id = @UserId",
                    new { UserId = userId }, transaction);
                var lastRecoveryAt = await conn.QueryFirstOrDefaultAsync<DateTime?>(
                    "SELECT MAX(AppliedAt) FROM StreakRecoveries WHERE UserId = @UserId",
                    new { UserId = userId }, transaction);
                var streak = GamificationRules.ApplyDailyStreak(user.Streak, user.LongestStreak,
                    user.LastVoteDate, lastRecoveryAt, utcNow, request.UseStreakRecovery);

                // Keep vote creation, counters, streak, and XP in one transaction.
                var voteId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Votes (PollId, OptionId, UserId, CreatedAt)
                      OUTPUT inserted.Id VALUES (@PollId, @OptionId, @UserId, @UtcNow)",
                    new { request.PollId, request.OptionId, UserId = userId, UtcNow = utcNow }, transaction);
                await conn.ExecuteAsync(@"
                    INSERT INTO XpEvents (UserId, Amount, SourceType, PollId, OccurredAt, IsValid, IsLeaderboardEligible)
                    VALUES (@UserId, @XpAwarded, 'Vote', @PollId, @UtcNow, 1, 1)",
                    new { UserId = userId, XpAwarded = xpAwarded, request.PollId, UtcNow = utcNow }, transaction);
                await conn.ExecuteAsync(
                    "UPDATE PollOptions SET VoteCount = VoteCount + 1 WHERE Id = @OptionId AND PollId = @PollId",
                    new { request.OptionId, request.PollId }, transaction);
                await conn.ExecuteAsync(
                    "UPDATE Polls SET TotalVotes = TotalVotes + 1 WHERE Id = @PollId",
                    new { request.PollId }, transaction);
                await conn.ExecuteAsync(@"
                    UPDATE o SET VotePercentage = CASE WHEN p.TotalVotes = 0 THEN 0
                        ELSE CAST(o.VoteCount AS FLOAT) / p.TotalVotes * 100 END
                    FROM PollOptions o JOIN Polls p ON p.Id = o.PollId WHERE o.PollId = @PollId",
                    new { request.PollId }, transaction);

                if (streak.RecoveryUsed)
                    await conn.ExecuteAsync(@"
                        INSERT INTO StreakRecoveries (UserId, MissedUtcDate, AppliedAt, PollId)
                        VALUES (@UserId, @MissedUtcDate, @UtcNow, @PollId)",
                        new { UserId = userId, MissedUtcDate = streak.LastVoteDate.AddDays(-1), UtcNow = utcNow, request.PollId }, transaction);

                var result = await conn.QuerySingleAsync<VoteRewardResult>(@"
                    UPDATE Users SET Xp = Xp + @XpAwarded, TotalVotes = TotalVotes + 1,
                        Streak = @Streak, LongestStreak = @LongestStreak, LastVoteDate = @LastVoteDate
                    OUTPUT inserted.Xp, inserted.Streak, inserted.LongestStreak, inserted.TotalVotes,
                        @XpAwarded AS XpAwarded, @StreakAdvanced AS StreakAdvanced,
                        CAST(1 AS bit) AS TodayComplete, @RecoveryEligible AS RecoveryEligible,
                        @RecoveryUsed AS RecoveryUsed, @MilestoneReached AS MilestoneReached,
                        inserted.LastVoteDate
                    WHERE Id = @UserId",
                    new { UserId = userId, XpAwarded = xpAwarded, streak.Streak, streak.LongestStreak,
                        streak.LastVoteDate, streak.StreakAdvanced, streak.RecoveryEligible,
                        streak.RecoveryUsed, streak.MilestoneReached }, transaction);
                result.NextRecoveryAt = streak.RecoveryUsed
                    ? utcNow.AddDays(GamificationRules.RecoveryCooldownDays)
                    : lastRecoveryAt?.AddDays(GamificationRules.RecoveryCooldownDays);
                if (await conn.ExecuteScalarAsync<string>("SELECT AnalyticsConsent FROM Users WHERE Id=@UserId", new { UserId = userId }, transaction) == "granted")
                    await _analytics.EnqueueAsync(conn, transaction, new AnalyticsEvent(Guid.NewGuid(), AnalyticsEventNames.GameRoundCompleted, $"usr_{userId}", AnalyticsRedactor.Serialize(new Dictionary<string, object?> { ["round_id"] = $"server-{userId}-{request.PollId}", ["surface"] = "api", ["outcome"] = "voted", ["xp_awarded"] = xpAwarded }, "round_id", "surface", "outcome", "xp_awarded"), utcNow, $"vote:{userId}:{request.PollId}:round-completed"));
                transaction.Commit();
                committed = true;

                var awards = await _achievementsRepo.AwardEligibleBadgesAsync(userId, utcNow);
                result.AwardedBadges = awards.AwardedBadges;
                if (awards.BonusXpAwarded > 0)
                {
                    result.Xp += awards.BonusXpAwarded;
                    result.XpAwarded += awards.BonusXpAwarded;
                }
                return (voteId, result);
            }
            catch
            {
                if (!committed) transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<Vote>> GetVotesByPollAsync(long pollId)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<Vote>(
                "SELECT * FROM Votes WHERE PollId = @PollId ORDER BY CreatedAt DESC", new { PollId = pollId });
        }
    }
}
