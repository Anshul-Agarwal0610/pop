using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IVotesRepository
    {
        /// <param name="userId">Authenticated user's ID.</param>
        Task<(long VoteId, VoteRewardResult Reward)> CastVoteAsync(
            CastVoteRequest request, long userId, int xpAwarded, DateTime utcNow);
        Task<IEnumerable<Vote>> GetVotesByPollAsync(long pollId);
    }
}
