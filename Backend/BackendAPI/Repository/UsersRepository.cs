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

        public async Task<LeaderboardResponse> GetRankingsAsync(
            LeaderboardPeriod period, int limit, int offset, long? currentUserId, DateTime utcNow)
        {
            limit = Math.Clamp(limit, 1, 100);
            offset = Math.Max(offset, 0);
            var window = LeaderboardWindow.For(period, utcNow);

            using var conn = _context.CreateConnection();
            var candidates = (await conn.QueryAsync<RankedCandidate>(
                @"WITH Totals AS (
                      SELECT e.UserId, SUM(CONVERT(BIGINT, e.Amount)) AS PeriodXp
                      FROM XpEvents e
                      WHERE e.IsValid = 1 AND e.IsLeaderboardEligible = 1
                        AND (@StartUtc IS NULL OR e.OccurredAt >= @StartUtc)
                        AND (@EndUtc IS NULL OR e.OccurredAt < @EndUtc)
                      GROUP BY e.UserId
                  ), Ranked AS (
                      SELECT u.Id, u.Username, u.DisplayName, u.AvatarUrl, u.Xp AS LifetimeXp,
                             t.PeriodXp,
                             RANK() OVER (ORDER BY t.PeriodXp DESC) AS Rank,
                             ROW_NUMBER() OVER (ORDER BY t.PeriodXp DESC, LOWER(u.Username), u.Id) AS RowNumber,
                             COUNT_BIG(*) OVER () AS TotalCount
                      FROM Totals t JOIN Users u ON u.Id = t.UserId
                      WHERE t.PeriodXp > 0
                  )
                  SELECT * FROM Ranked
                  WHERE (RowNumber > @Offset AND RowNumber <= @Offset + @Limit + 1)
                     OR Id = @CurrentUserId
                  ORDER BY RowNumber",
                new { window.StartUtc, window.EndUtc, Limit = limit, Offset = offset, CurrentUserId = currentUserId })).ToList();

            var page = candidates.Where(x => x.RowNumber > offset && x.RowNumber <= offset + limit).Cast<LeaderboardRow>().ToList();
            var current = currentUserId == null ? null : candidates.FirstOrDefault(x => x.Id == currentUserId.Value);
            var badgeIds = page.Select(x => x.Id).Append(current?.Id ?? 0).Where(x => x > 0).Distinct();
            var badges = await _achievementsRepo.GetBadgesForUsersAsync(badgeIds);
            foreach (var row in page.Append(current).Where(x => x != null).DistinctBy(x => x!.Id))
                if (badges.TryGetValue(row!.Id, out var userBadges)) row.Badges = userBadges;

            return new LeaderboardResponse
            {
                Rows = page,
                CurrentUser = current,
                Period = period,
                PeriodStartUtc = window.StartUtc,
                PeriodEndUtc = window.EndUtc,
                NextResetAtUtc = period == LeaderboardPeriod.Weekly ? window.EndUtc : null,
                Limit = limit,
                Offset = offset,
                HasMore = candidates.Any(x => x.RowNumber == (long)offset + limit + 1)
            };
        }

        private sealed class RankedCandidate : LeaderboardRow
        {
            public long RowNumber { get; set; }
            public long TotalCount { get; set; }
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

        public async Task<VoteRewardResult> ApplyVoteRewardAsync(long userId, long pollId, int xpToAdd, DateTime utcNow, bool leaderboardEligible = true)
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

            await conn.ExecuteAsync(
                @"INSERT INTO XpEvents (UserId, Amount, SourceType, PollId, OccurredAt, IsValid, IsLeaderboardEligible)
                  VALUES (@UserId, @Amount, 'Vote', @PollId, @OccurredAt, 1, @Eligible)",
                new { UserId = userId, Amount = xpToAdd, PollId = pollId, OccurredAt = utcNow, Eligible = leaderboardEligible },
                transaction);

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
