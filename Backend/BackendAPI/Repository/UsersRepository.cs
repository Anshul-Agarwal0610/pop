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

        public UsersRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetLeaderboardAsync(int count = 20)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryAsync<User>(
                "SELECT TOP (@Count) * FROM Users ORDER BY Xp DESC",
                new { Count = count }
            );
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            using var conn = _context.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Id = @Id",
                new { Id = id }
            );
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
                user.LastVoteDate,
                utcNow);

            var updated = await conn.QuerySingleAsync<VoteRewardResult>(
                @"UPDATE Users
                  SET Xp = Xp + @XpAwarded,
                      TotalVotes = TotalVotes + 1,
                      Streak = @Streak,
                      LastVoteDate = @LastVoteDate
                  OUTPUT inserted.Xp,
                         inserted.Streak,
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
                    streak.StreakAdvanced,
                    streak.LastVoteDate
                },
                transaction
            );

            transaction.Commit();
            return updated;
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
    }
}
