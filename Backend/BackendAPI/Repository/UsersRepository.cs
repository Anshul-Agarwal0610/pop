using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
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

        public async Task UpdateXpAsync(long userId, int xpToAdd)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE Users SET Xp = Xp + @Xp, TotalVotes = TotalVotes + 1 WHERE Id = @Id",
                new { Xp = xpToAdd, Id = userId }
            );
        }

        public async Task UpdateStreakAsync(long userId)
        {
            using var conn = _context.CreateConnection();
            await conn.ExecuteAsync(
                "UPDATE Users SET Streak = Streak + 1 WHERE Id = @Id",
                new { Id = userId }
            );
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
