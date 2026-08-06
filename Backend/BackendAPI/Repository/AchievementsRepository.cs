using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class AchievementsRepository : IAchievementsRepository
    {
        private readonly DapperContext _context;

        public AchievementsRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserBadge>> GetUserBadgesAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<UserBadge>(
                BadgeSelectSql + " WHERE ub.UserId = @UserId ORDER BY ub.AwardedAt DESC",
                new { UserId = userId });
        }

        public async Task<Dictionary<long, List<UserBadge>>> GetBadgesForUsersAsync(IEnumerable<long> userIds)
        {
            var ids = userIds.Distinct().ToArray();
            if (ids.Length == 0) return new Dictionary<long, List<UserBadge>>();

            using var conn = _context.CreateConnection();
            var badges = await conn.QueryAsync<UserBadge>(
                BadgeSelectSql + " WHERE ub.UserId IN @UserIds ORDER BY ub.AwardedAt DESC",
                new { UserIds = ids });

            return badges
                .GroupBy(badge => badge.UserId)
                .ToDictionary(group => group.Key, group => group.ToList());
        }

        public async Task<AchievementAwardResult> AwardEligibleBadgesAsync(long userId, DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var user = await conn.QuerySingleAsync<User>(
                    "SELECT Id, Xp, Streak, TotalVotes, PollsCreated FROM Users WHERE Id = @UserId",
                    new { UserId = userId },
                    transaction);

                var completedChallenges = await conn.ExecuteScalarAsync<int>(
                    @"SELECT COUNT(1)
                      FROM UserChallengeProgress
                      WHERE UserId = @UserId AND IsCompleted = 1",
                    new { UserId = userId },
                    transaction);

                var eligible = (await conn.QueryAsync<AchievementBadge>(
                    @"SELECT *
                      FROM AchievementBadges
                      WHERE (RuleType = @VoteCountRule AND Threshold <= @TotalVotes)
                         OR (RuleType = @StreakRule AND Threshold <= @Streak)
                         OR (RuleType = @PollCreationRule AND Threshold <= @PollsCreated)
                         OR (RuleType = @ChallengeCompletionRule AND Threshold <= @CompletedChallenges)",
                    new
                    {
                        VoteCountRule = AchievementRuleType.VoteCount,
                        StreakRule = AchievementRuleType.Streak,
                        PollCreationRule = AchievementRuleType.PollCreation,
                        ChallengeCompletionRule = AchievementRuleType.ChallengeCompletion,
                        user.TotalVotes,
                        user.Streak,
                        user.PollsCreated,
                        CompletedChallenges = completedChallenges
                    },
                    transaction)).ToList();

                var awarded = new List<UserBadge>();
                var bonusXp = 0;

                foreach (var badge in eligible)
                {
                    var inserted = await conn.ExecuteScalarAsync<long?>(
                        @"IF NOT EXISTS (
                              SELECT 1 FROM UserBadges
                              WHERE UserId = @UserId AND BadgeId = @BadgeId
                          )
                          BEGIN
                              INSERT INTO UserBadges (UserId, BadgeId, AwardedAt)
                              VALUES (@UserId, @BadgeId, @AwardedAt);
                              SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
                          END
                          ELSE
                          BEGIN
                              SELECT CAST(NULL AS BIGINT);
                          END",
                        new { UserId = userId, BadgeId = badge.Id, AwardedAt = utcNow },
                        transaction);

                    if (inserted == null) continue;

                    if (badge.RewardXp > 0)
                    await conn.ExecuteAsync(
                        @"INSERT INTO RewardEvents
                              (UserId, RuleCode, RuleVersion, Reason, SourceType, SourceReference, SourceKey, Value, EventType, CreatedAt)
                          VALUES
                              (@UserId, @RuleCode, 1, @Reason, 'achievement', @SourceReference, @SourceKey, @Value, 'Grant', @AwardedAt)",
                        new
                        {
                            UserId = userId,
                            RuleCode = $"achievement.{badge.Code}",
                            Reason = $"Achievement awarded: {badge.Name}",
                            SourceReference = badge.Id.ToString(),
                            SourceKey = $"achievement:{badge.Id}:award",
                            Value = badge.RewardXp,
                            AwardedAt = utcNow
                        }, transaction);

                    bonusXp += badge.RewardXp;
                    awarded.Add(new UserBadge
                    {
                        Id = inserted.Value,
                        UserId = userId,
                        BadgeId = badge.Id,
                        Code = badge.Code,
                        Name = badge.Name,
                        Description = badge.Description,
                        Icon = badge.Icon,
                        AwardedAt = utcNow
                    });
                }

                if (bonusXp > 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE Users SET Xp = Xp + @BonusXp WHERE Id = @UserId",
                        new { UserId = userId, BonusXp = bonusXp },
                        transaction);
                }

                transaction.Commit();

                return new AchievementAwardResult
                {
                    AwardedBadges = awarded,
                    BonusXpAwarded = bonusXp
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private const string BadgeSelectSql =
            @"SELECT
                  ub.Id,
                  ub.UserId,
                  ub.BadgeId,
                  b.Code,
                  b.Name,
                  b.Description,
                  b.Icon,
                  ub.AwardedAt
              FROM UserBadges ub
              JOIN AchievementBadges b ON b.Id = ub.BadgeId";
    }
}
