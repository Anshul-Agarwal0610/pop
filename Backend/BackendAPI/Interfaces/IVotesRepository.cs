using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IVotesRepository
    {
        /// <param name="userId">Authenticated user's ID — null for anonymous votes.</param>
        Task<bool> CastVoteAsync(CastVoteRequest request, long? userId);
        Task<IEnumerable<Vote>> GetVotesByPollAsync(long pollId);
    }
}
