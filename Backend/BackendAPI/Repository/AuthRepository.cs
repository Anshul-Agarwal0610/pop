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
        private const string CurrentTermsVersion = "2026-10-11";
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
            if (!request.AcceptTerms)
                throw new ArgumentException("You must accept the Terms of Use and Community Guidelines.");
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
                @"INSERT INTO Users (Username, DisplayName, Email, PasswordHash, AuthProvider, Xp, Streak, TotalVotes, PollsCreated, CreatedAt, TermsAcceptedAt, TermsVersion)
                  VALUES (@Username, @DisplayName, NULL, @PasswordHash, 'local', 0, 0, 0, 0, GETUTCDATE(), GETUTCDATE(), @TermsVersion);
                  SELECT SCOPE_IDENTITY();",
                new { request.Username, request.DisplayName, PasswordHash = hash, TermsVersion = CurrentTermsVersion }
            );

            var user = await GetUserResponseAsync(userId);
            return new AuthResponse { Token = GenerateToken(user!), User = user! };
        }

        // ── Login ─────────────────────────────────────────────────────────────

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            using var conn = _context.CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id, PasswordHash FROM Users WHERE Username = @Username AND AuthProvider = 'local' AND AccountType = @AccountType",
                new { request.Username, AccountType = AccountTypes.Personal }
            );

            string? storedHash = row?.PasswordHash as string;
            if (row == null || storedHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
                throw new UnauthorizedAccessException("Invalid username or password.");

            long rowId = (long)row.Id;
            var user = await GetUserResponseAsync(rowId);
            return new AuthResponse { Token = GenerateToken(user!), User = user! };
        }

        public async Task<AuthResponse> LoginBusinessAsync(LoginRequest request)
        {
            using var conn = _context.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id, PasswordHash FROM Users WHERE Username = @Username AND AuthProvider = 'local' AND AccountType = @AccountType",
                new { request.Username, AccountType = AccountTypes.Business });
            string? storedHash = row?.PasswordHash as string;
            if (row == null || storedHash == null || !BCrypt.Net.BCrypt.Verify(request.Password, storedHash))
                throw new UnauthorizedAccessException("Invalid business username or password.");
            var user = await GetUserResponseAsync((long)row.Id);
            return new AuthResponse { Token = GenerateToken(user!), User = user! };
        }

        public async Task<AuthResponse> RegisterBusinessAsync(RegisterBusinessRequest request)
        {
            if (!request.AcceptTerms) throw new ArgumentException("You must accept the Terms of Use and Community Guidelines.");
            if (request.Password != request.ConfirmPassword) throw new ArgumentException("Passwords do not match.");
            if (string.IsNullOrWhiteSpace(request.BusinessName)) throw new ArgumentException("Business name is required.");
            var allowedPlans = new[] { "Starter", "Growth", "Pro" };
            var plan = allowedPlans.FirstOrDefault(value => value.Equals(request.SubscriptionPlan, StringComparison.OrdinalIgnoreCase)) ?? "Starter";
            using var conn = _context.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Users WHERE Username = @Username", new { request.Username }, transaction) > 0)
                    throw new InvalidOperationException("Username is already taken.");
                var userId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Users (Username, DisplayName, Email, PasswordHash, AuthProvider, AccountType, Xp, Streak, TotalVotes, PollsCreated, CreatedAt, TermsAcceptedAt, TermsVersion)
                      VALUES (@Username, @DisplayName, NULL, @PasswordHash, 'local', @AccountType, 0, 0, 0, 0, GETUTCDATE(), GETUTCDATE(), @TermsVersion);
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                    new { request.Username, request.DisplayName, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), AccountType = AccountTypes.Business, TermsVersion = CurrentTermsVersion }, transaction);
                await conn.ExecuteAsync(
                    @"INSERT INTO BusinessAccounts (OwnerUserId, Name, WebsiteUrl, Status, SubscriptionPlan, SubscriptionStatus, TrialEndsAt, CreatedAt)
                      VALUES (@OwnerUserId, @Name, @WebsiteUrl, @Status, @Plan, @SubscriptionStatus, DATEADD(day, 14, GETUTCDATE()), GETUTCDATE())",
                    new { OwnerUserId = userId, Name = request.BusinessName.Trim(), WebsiteUrl = string.IsNullOrWhiteSpace(request.WebsiteUrl) ? null : request.WebsiteUrl.Trim(), Status = BusinessAccountStatus.Active, Plan = plan, SubscriptionStatus = BusinessSubscriptionStatus.Trialing }, transaction);
                transaction.Commit();
                var user = await GetUserResponseAsync(userId);
                return new AuthResponse { Token = GenerateToken(user!), User = user! };
            }
            catch { transaction.Rollback(); throw; }
        }

        // ── Google Login ──────────────────────────────────────────────────────

        public async Task<AuthResponse> GoogleLoginAsync(string email, string displayName, string? avatarUrl, bool acceptTerms)
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
                if (!acceptTerms)
                    throw new ArgumentException("You must accept the Terms of Use and Community Guidelines.");
                // Auto-register on first Google sign-in
                var username = email.Split('@')[0].ToLower().Replace(".", "_");
                // Ensure username uniqueness
                var taken = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id FROM Users WHERE Username = @Username", new { Username = username }
                );
                if (taken != null) username = $"{username}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                userId = await conn.ExecuteScalarAsync<long>(
                    @"INSERT INTO Users (Username, DisplayName, Email, PasswordHash, AvatarUrl, AuthProvider, Xp, Streak, TotalVotes, PollsCreated, CreatedAt, TermsAcceptedAt, TermsVersion)
                      VALUES (@Username, @DisplayName, @Email, NULL, @AvatarUrl, 'google', 0, 0, 0, 0, GETUTCDATE(), GETUTCDATE(), @TermsVersion);
                      SELECT SCOPE_IDENTITY();",
                    new { Username = username, DisplayName = displayName, Email = email, AvatarUrl = avatarUrl, TermsVersion = CurrentTermsVersion }
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
                @"SELECT Id, Username, DisplayName, Email, AvatarUrl, AuthProvider, AccountType,
                         Xp, Streak, LongestStreak, TotalVotes, PollsCreated, LastVoteDate, CreatedAt, IsAdmin
                  FROM Users WHERE Id = @Id AND AuthProvider <> 'deleted'",
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
                new Claim("accountType", user.AccountType),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : user.AccountType),
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
