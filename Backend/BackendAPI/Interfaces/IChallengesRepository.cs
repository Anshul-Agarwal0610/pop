using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IChallengesRepository
{
    Task EnsureCurrentOccurrencesAsync(DateTime utcNow);
    Task<IEnumerable<UserChallenge>> GetForUserAsync(long userId, DateTime utcNow, string state = "active");
    Task<IEnumerable<UserChallenge>> GetActiveForUserAsync(long userId, DateTime utcNow);
    Task<IEnumerable<UserChallenge>> AdvanceForVoteAsync(long userId, long voteId, Poll poll, DateTime utcNow);
}
