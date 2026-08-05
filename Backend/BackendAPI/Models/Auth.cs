namespace BackendAPI.Models
{
    // ── Requests ────────────────────────────────────────────────────────────

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        /// <summary>The ID token returned by Google Sign-In on the frontend.</summary>
        public string IdToken { get; set; } = string.Empty;
    }

    // ── Responses ───────────────────────────────────────────────────────────

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserResponse User { get; set; } = new();
    }

    /// <summary>Safe user object — never includes PasswordHash.</summary>
    public class UserResponse
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string AuthProvider { get; set; } = "local";
        public int Xp { get; set; }
        public int Streak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalVotes { get; set; }
        public int PollsCreated { get; set; }
        public DateTime? LastVoteDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Level => Xp / 1000 + 1;
        public List<UserBadge> Badges { get; set; } = new();
    }
}
