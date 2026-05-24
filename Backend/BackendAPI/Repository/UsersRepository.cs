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
                  ORDER BY v.CreatedAt DESC",
                new { UserId = userId, Count = count }
            );
        }
    }
}
