using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IAuthRepository
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterBusinessAsync(RegisterBusinessRequest request);
        Task<AuthResponse> LoginBusinessAsync(LoginRequest request);
        Task<AuthResponse> GoogleLoginAsync(string email, string displayName, string? avatarUrl, bool acceptTerms);
        Task<UserResponse?> GetByIdAsync(long id);
    }
}
