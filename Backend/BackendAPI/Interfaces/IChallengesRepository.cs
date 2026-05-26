using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IChallengesRepository
    {
        Task EnsureDailyChallengeAsync(DateTime utcNow);
        Task<IEnumerable<UserChallenge>> GetActiveForUserAsync(long userId, DateTime utcNow);
        Task<IEnumerable<UserChallenge>> AdvanceForVoteAsync(long userId, Poll poll, DateTime utcNow);
    }
}
