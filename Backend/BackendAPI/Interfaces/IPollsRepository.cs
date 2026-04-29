using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IPollsRepository
    {
        Task<IEnumerable<Poll>> GetAllAsync();
        Task<Poll?> GetByIdAsync(long id);
        Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10);
        Task<IEnumerable<Poll>> GetRecentAsync(int count = 10);
        Task<long> CreateAsync(CreatePollRequest request);
        Task<bool> DeleteAsync(long id);
        Task UpdateTrendingAsync();
    }
}
