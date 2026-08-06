using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Dapper;

namespace BackendAPI.Repository
{
    public class UsersRepository : IUsersRepository
    {
        private readonly DapperContext _context;
        private readonly IAchievementsRepository _achievementsRepo;

        public UsersRepository(DapperContext context, IAchievementsRepository achievementsRepo)
        {
            _context = context;
            _achievementsRepo = achievementsRepo;
        }

        public async Task<IEnumerable<User>> GetLeaderboardAsync(int count = 20)
        {
            using var conn = _context.CreateConnection();
            var users = (await conn.QueryAsync<User>(
                "SELECT TOP (@Count) * FROM Users ORDER BY Xp DESC",
                new { Count = count }
            )).ToList();

            var badgesByUser = await _achievementsRepo.GetBadgesForUsersAsync(users.Select(user => user.Id));
            foreach (var user in users)
            {
                if (badgesByUser.TryGetValue(user.Id, out var badges))
                    user.Badges = badges;
            }

            return users;
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            using var conn = _context.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Id = @Id",
                new { Id = id }
            );

            if (user != null)
                user.Badges = (await _achievementsRepo.GetUserBadgesAsync(id)).ToList();

            return user;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Username = @Username",
                new { Username = username }
            );
        }

        public async Task<long> CreateAsync(CreateUserRequest request)
        {
            using var conn = _context.CreateConnection();
            return await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO Users (Username, DisplayName, Xp, Streak, TotalVotes, PollsCreated, CreatedAt)
                  VALUES (@Username, @DisplayName, 0, 0, 0, 0, GETUTCDATE());
                  SELECT SCOPE_IDENTITY();",
                new { request.Username, request.DisplayName }
            );
        }

        public async Task IncrementPollsCreatedAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE Users SET PollsCreated = PollsCreated + 1 WHERE Id = @UserId",
                new { UserId = userId });

            await _achievementsRepo.AwardEligibleBadgesAsync(userId, DateTime.UtcNow);
        }

        public async Task<VoteRewardResult> ApplyVoteRewardAsync(long userId, int xpToAdd, DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            var user = await conn.QuerySingleAsync<User>(
                @"SELECT Id, Xp, Streak, TotalVotes, LastVoteDate
                  FROM Users WITH (UPDLOCK, ROWLOCK)
                  WHERE Id = @Id",
                new { Id = userId },
                transaction
            );

            var streak = GamificationRules.ApplyDailyStreak(
                user.Streak,
                user.LongestStreak,
                user.LastVoteDate,
                null,
                utcNow);

            var updated = await conn.QuerySingleAsync<VoteRewardResult>(
                @"UPDATE Users
                  SET Xp = Xp + @XpAwarded,
                      TotalVotes = TotalVotes + 1,
                      Streak = @Streak,
                      LongestStreak = @LongestStreak,
                      LastVoteDate = @LastVoteDate
                  OUTPUT inserted.Xp,
                         inserted.Streak,
                         inserted.LongestStreak,
                         inserted.TotalVotes,
                         @XpAwarded AS XpAwarded,
                         @StreakAdvanced AS StreakAdvanced,
                         inserted.LastVoteDate
                  WHERE Id = @Id",
                new
                {
                    Id = userId,
                    XpAwarded = xpToAdd,
                    streak.Streak,
                    streak.LongestStreak,
                    streak.StreakAdvanced,
                    streak.LastVoteDate
                },
                transaction
            );

            transaction.Commit();

            var awards = await _achievementsRepo.AwardEligibleBadgesAsync(userId, utcNow);
            updated.AwardedBadges = awards.AwardedBadges;
            if (awards.BonusXpAwarded > 0)
            {
                updated.Xp += awards.BonusXpAwarded;
                updated.XpAwarded += awards.BonusXpAwarded;
            }

            return updated;
        }

        public async Task<StreakStatus?> GetStreakStatusAsync(long userId, DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<(int Streak, int LongestStreak, DateTime? LastVoteDate, DateTime? LastRecoveryAt)>(@"
                SELECT u.Streak, u.LongestStreak, u.LastVoteDate,
                    (SELECT MAX(r.AppliedAt) FROM StreakRecoveries r WHERE r.UserId = u.Id) AS LastRecoveryAt
                FROM Users u WHERE u.Id = @UserId", new { UserId = userId });
            if (row == default) return null;
            var today = utcNow.ToUniversalTime().Date;
            var recoverable = row.LastVoteDate?.Date == today.AddDays(-2);
            var available = !row.LastRecoveryAt.HasValue || row.LastRecoveryAt.Value <= utcNow.AddDays(-GamificationRules.RecoveryCooldownDays);
            return new StreakStatus {
                Streak = row.Streak, LongestStreak = row.LongestStreak,
                TodayComplete = row.LastVoteDate?.Date == today, LastVoteDate = row.LastVoteDate,
                RecoveryEligible = recoverable && available,
                NextRecoveryAt = row.LastRecoveryAt?.AddDays(GamificationRules.RecoveryCooldownDays)
            };
        }

        // US-22: Vote history ─────────────────────────────────────────────────
        public async Task<IEnumerable<VoteHistoryItem>> GetVoteHistoryAsync(long userId, int count = 20)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<VoteHistoryItem>(
                @"SELECT TOP (@Count)
                      p.Id           AS PollId,
                      p.Question,
                      p.Category,
                      o.Text         AS VotedOptionText,
                      p.TotalVotes,
                      v.CreatedAt    AS VotedAt
                  FROM Votes v
                  JOIN Polls       p ON p.Id = v.PollId
                  JOIN PollOptions o ON o.Id = v.OptionId
                  WHERE v.UserId = @UserId
                    AND COALESCE(p.IsPrivate, 0) = 0
                    AND COALESCE(p.IsWellness, 0) = 0
                    AND p.Category <> 'Health'
                  ORDER BY v.CreatedAt DESC",
                new { UserId = userId, Count = count }
            );
        }

        public async Task<IEnumerable<UserCategoryPreference>> GetCategoryPreferencesAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<UserCategoryPreference>(
                @"WITH ExplicitPrefs AS (
                    SELECT Category
                    FROM UserCategoryPreferences
                    WHERE UserId = @UserId
                  ),
                  Activity AS (
                    SELECT p.Category, COUNT_BIG(*) AS VoteCount
                    FROM Votes v
                    JOIN Polls p ON p.Id = v.PollId
                    WHERE v.UserId = @UserId
                      AND p.Category <> 'Health'
                    GROUP BY p.Category
                  )
                  SELECT
                    COALESCE(pref.Category, activity.Category) AS Category,
                    CAST(CASE WHEN pref.Category IS NULL THEN 0 ELSE 1 END AS bit) AS IsExplicit,
                    CAST(COALESCE(activity.VoteCount, 0) AS int) AS VoteCount
                  FROM ExplicitPrefs pref
                  FULL OUTER JOIN Activity activity ON LOWER(activity.Category) = LOWER(pref.Category)
                  ORDER BY IsExplicit DESC, VoteCount DESC, Category ASC",
                new { UserId = userId });
        }

        public async Task<IEnumerable<UserCategoryPreference>> ReplaceCategoryPreferencesAsync(
            long userId,
            IEnumerable<string> categories)
        {
            var normalized = categories
                .Select(CategoryCatalog.NormalizeName)
                .Where(category => !category.Equals("Health", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            await conn.ExecuteAsync(
                "DELETE FROM UserCategoryPreferences WHERE UserId = @UserId",
                new { UserId = userId },
                transaction);

            foreach (var category in normalized)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO UserCategoryPreferences (UserId, Category, CreatedAt)
                      VALUES (@UserId, @Category, GETUTCDATE())",
                    new { UserId = userId, Category = category },
                    transaction);
            }

            transaction.Commit();
            return await GetCategoryPreferencesAsync(userId);
        }

        public async Task ResetCategoryPreferencesAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM UserCategoryPreferences WHERE UserId = @UserId",
                new { UserId = userId });
        }

        public async Task<UserProgression?> GetProgressionAsync(long userId, DateTime utcNow)
        {
            using var conn = _context.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT Id, Xp, Streak, LastVoteDate FROM Users WHERE Id = @UserId",
                new { UserId = userId });
            return user == null ? null : GamificationRules.Progression(user, utcNow);
        }

        public async Task<WeeklyLeaderboardResponse> GetWeeklyLeaderboardAsync(long userId, int count, DateTime utcNow)
        {
            count = Math.Clamp(count, 1, 20);
            var today = utcNow.Date;
            var mondayOffset = ((int)today.DayOfWeek + 6) % 7;
            var weekStart = today.AddDays(-mondayOffset);
            var weekEnd = weekStart.AddDays(7);

            using var conn = _context.CreateConnection();
            using var results = await conn.QueryMultipleAsync(
                @"WITH Scores AS (
                    SELECT u.Id AS UserId, u.Username, u.DisplayName, COUNT_BIG(v.Id) AS Score
                    FROM Users u
                    JOIN Votes v ON v.UserId = u.Id AND v.CreatedAt >= @WeekStart AND v.CreatedAt < @WeekEnd
                    JOIN Polls p ON p.Id = v.PollId
                    WHERE COALESCE(p.IsPrivate, 0) = 0 AND COALESCE(p.IsWellness, 0) = 0 AND p.Category <> 'Health'
                    GROUP BY u.Id, u.Username, u.DisplayName
                  )
                  SELECT TOP (@Count) UserId, Username, DisplayName,
                         CAST(DENSE_RANK() OVER (ORDER BY Score DESC) AS int) AS Rank,
                         CAST(Score AS int) AS Score
                  FROM Scores
                  ORDER BY Rank, UserId;

                  WITH Scores AS (
                    SELECT u.Id AS UserId, u.Username, u.DisplayName, COUNT_BIG(v.Id) AS Score
                    FROM Users u
                    JOIN Votes v ON v.UserId = u.Id AND v.CreatedAt >= @WeekStart AND v.CreatedAt < @WeekEnd
                    JOIN Polls p ON p.Id = v.PollId
                    WHERE COALESCE(p.IsPrivate, 0) = 0 AND COALESCE(p.IsWellness, 0) = 0 AND p.Category <> 'Health'
                    GROUP BY u.Id, u.Username, u.DisplayName
                  ), Ranked AS (
                    SELECT UserId, Username, DisplayName,
                           CAST(DENSE_RANK() OVER (ORDER BY Score DESC) AS int) AS Rank,
                           CAST(Score AS int) AS Score
                    FROM Scores
                  )
                  SELECT UserId, Username, DisplayName, Rank, Score
                  FROM Ranked
                  WHERE UserId = @UserId;",
                new { WeekStart = weekStart, WeekEnd = weekEnd, Count = count, UserId = userId });

            var entries = (await results.ReadAsync<WeeklyLeaderboardEntry>()).ToList();
            var currentUser = await results.ReadFirstOrDefaultAsync<WeeklyLeaderboardEntry>();

            return new WeeklyLeaderboardResponse
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                Entries = entries,
                CurrentUser = currentUser
            };
        }
    }
}
