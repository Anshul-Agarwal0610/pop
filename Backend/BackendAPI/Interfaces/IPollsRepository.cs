using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IPollsRepository
    {
        /// <param name="userId">When provided, HasVoted / UserVotedOptionId are populated.</param>
        Task<IEnumerable<Poll>> GetAllAsync(long? userId = null);
        Task<Poll?> GetByIdAsync(long id);
        Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10, long? userId = null);
        Task<IEnumerable<Poll>> GetRecentAsync(int count = 10);
        Task<long> CreateAsync(CreatePollRequest request);
        Task<bool> DeleteAsync(long id);
        Task UpdateTrendingAsync();
    }
}
