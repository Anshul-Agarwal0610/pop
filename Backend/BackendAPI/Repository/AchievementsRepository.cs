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

        public async Task<AchievementOverview> GetOverviewAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            var user = await conn.QuerySingleAsync<User>(
                "SELECT Id, Streak, TotalVotes, PollsCreated FROM Users WHERE Id = @UserId",
                new { UserId = userId });
            var completedChallenges = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM UserChallengeProgress WHERE UserId = @UserId AND IsCompleted = 1",
                new { UserId = userId });
            var recent = (await conn.QueryAsync<UserBadge>(
                BadgeSelectSql + " WHERE ub.UserId = @UserId ORDER BY ub.AwardedAt DESC",
                new { UserId = userId })).Take(3).ToList();
            var unearned = (await conn.QueryAsync<AchievementBadge>(
                @"SELECT b.* FROM AchievementBadges b
                  WHERE NOT EXISTS (SELECT 1 FROM UserBadges ub WHERE ub.UserId = @UserId AND ub.BadgeId = b.Id)",
                new { UserId = userId })).ToList();

            int CurrentValue(string rule) => rule switch
            {
                AchievementRuleType.VoteCount => user.TotalVotes,
                AchievementRuleType.Streak => user.Streak,
                AchievementRuleType.PollCreation => user.PollsCreated,
                AchievementRuleType.ChallengeCompletion => completedChallenges,
                _ => 0
            };

            var next = unearned.Select(b =>
            {
                var current = CurrentValue(b.RuleType);
                return new AchievementProgress
                {
                    BadgeId = b.Id, Code = b.Code, Name = b.Name, Description = b.Description,
                    Icon = b.Icon, RuleType = b.RuleType, CurrentValue = current, Threshold = b.Threshold,
                    ProgressPercent = b.Threshold <= 0 ? 100 : Math.Min(100, current * 100d / b.Threshold),
                    RewardXp = b.RewardXp
                };
            }).OrderByDescending(b => b.ProgressPercent).ThenBy(b => b.Threshold - b.CurrentValue).Take(3).ToList();

            return new AchievementOverview { RecentlyEarned = recent, NextAchievable = next, AllEarned = unearned.Count == 0 };
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
