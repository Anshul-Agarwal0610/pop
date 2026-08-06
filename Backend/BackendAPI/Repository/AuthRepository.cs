using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BackendAPI.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _config;
        private readonly IAchievementsRepository _achievementsRepo;

        public AuthRepository(
            DapperContext context,
            IConfiguration config,
            IAchievementsRepository achievementsRepo)
        {
            _context = context;
            _config = config;
            _achievementsRepo = achievementsRepo;
        }

        // ── Register ─────────────────────────────────────────────────────────

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                throw new ArgumentException("Passwords do not match.");

            using var conn = _context.CreateConnection();

            var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id FROM Users WHERE Username = @Username",
                new { request.Username }
            );
            if (existing != null)
                throw new InvalidOperationException("Username is already taken.");

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var userId = await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO Users (Username, DisplayName, Email, PasswordHash, AuthProvider, Xp, Streak, TotalVotes, PollsCreated, CreatedAt)
                  VALUES (@Username, @DisplayName, NULL, @PasswordHash, 'local', 0, 0, 0, 0, GETUTCDATE());
                  SELECT SCOPE_IDENTITY();",
                new { request.Username, request.DisplayName, PasswordHash = hash }
            );

            var user = await GetUserResponseAsync(userId);
            return new AuthResponse { Token = GenerateToken(user!), User = user! };
        }

        // ── Login ─────────────────────────────────────────────────────────────

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            using var conn = _context.CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id, PasswordHash FROM Users WHERE Username = @Username AND AuthProvider = 'local'",
                new { request.Username }
            );

            string? storedHash = row?.PasswordHash as string;
            if (row == null || storedHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
                throw new UnauthorizedAccessException("Invalid username or password.");

            long rowId = (long)row.Id;
            var user = await GetUserResponseAsync(rowId);
            return new AuthResponse { Token = GenerateToken(user!), User = user! };
        }

        // ── Google Login ──────────────────────────────────────────────────────

        public async Task<AuthResponse> GoogleLoginAsync(string email, string displayName, string? avatarUrl)
        {
            using var conn = _context.CreateConnection();

            // Find existing Google user by email
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id FROM Users WHERE Email = @Email AND AuthProvider = 'google'",
                new { Email = email }
            );

            long userId;

            if (row == null)
            {
                // Auto-register on first Google sign-in
                var username = email.Split('@')[0].ToLower().Replace(".", "_");
                // Ensure username uniqueness
                var taken = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id FROM Users WHERE Username = @Username", new { Username = username }
                );
                if (taken != null) username = $"{username}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                userId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Users (Username, DisplayName, Email, PasswordHash, AvatarUrl, AuthProvider, Xp, Streak, TotalVotes, PollsCreated, CreatedAt)
                      VALUES (@Username, @DisplayName, @Email, NULL, @AvatarUrl, 'google', 0, 0, 0, 0, GETUTCDATE());
                      SELECT SCOPE_IDENTITY();",
                    new { Username = username, DisplayName = displayName, Email = email, AvatarUrl = avatarUrl }
                );
            }
            else
            {
                userId = (long)row.Id;
                // Update display name / avatar in case they changed
                await conn.ExecuteAsync(
                    "UPDATE Users SET DisplayName = @DisplayName, AvatarUrl = @AvatarUrl WHERE Id = @Id",
                    new { DisplayName = displayName, AvatarUrl = avatarUrl, Id = userId }
                );
            }

            var user = await GetUserResponseAsync(userId);
            return new AuthResponse { Token = GenerateToken(user!), User = user! };
        }

        // ── Get by ID ─────────────────────────────────────────────────────────

        public async Task<UserResponse?> GetByIdAsync(long id)
            => await GetUserResponseAsync(id);

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<UserResponse?> GetUserResponseAsync(long id)
        {
            using var conn = _context.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<UserResponse>(
                @"SELECT Id, Username, DisplayName, Email, AvatarUrl, AuthProvider,
                         Xp, Streak, LongestStreak, TotalVotes, PollsCreated, LastVoteDate, CreatedAt, IsAdmin
                  FROM Users WHERE Id = @Id",
                new { Id = id }
            );

            if (user != null)
                user.Badges = (await _achievementsRepo.GetUserBadgesAsync(id)).ToList();

            return user;
        }

        private string GenerateToken(UserResponse user)
        {
            var jwtKey = _config["Jwt:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("displayName", user.DisplayName),
                new Claim("authProvider", user.AuthProvider),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer:   _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims:   claims,
                expires:  DateTime.UtcNow.AddHours(double.Parse(_config["Jwt:ExpiryInHours"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
