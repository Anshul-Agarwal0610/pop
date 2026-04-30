using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IVotesRepository
    {
        Task<bool> CastVoteAsync(CastVoteRequest request);
        Task<IEnumerable<Vote>> GetVotesByPollAsync(long pollId);
    }
}
