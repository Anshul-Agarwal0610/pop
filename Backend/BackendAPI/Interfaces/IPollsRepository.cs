using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IPollsRepository
    {
        /// <param name="userId">When provided, HasVoted / UserVotedOptionId are populated.</param>
        Task<IEnumerable<Poll>> GetAllAsync(long? userId = null, string? category = null);
        Task<Poll?> GetByIdAsync(long id, long? userId = null);
        Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10, long? userId = null, string? category = null);
        Task<IEnumerable<Poll>> SearchAsync(string query, string? category = null, long? userId = null);
        Task<IEnumerable<Poll>> GetRecentAsync(int count = 10);
        Task<long> CreateAsync(CreatePollRequest request, long? createdByUserId = null);
        Task<bool> DeleteAsync(long id);
        Task UpdateTrendingAsync();
    }
}
