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
    }
}
