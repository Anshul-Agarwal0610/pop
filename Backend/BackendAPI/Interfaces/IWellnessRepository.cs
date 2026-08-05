using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IWellnessRepository
    {
        Task<IEnumerable<Poll>> GetActivePollsAsync(long userId);
        Task<WellnessOverview> GetOverviewAsync(long userId);
        Task<WellnessResponse?> CreateResponseAsync(long userId, CreateWellnessResponseRequest request);
        Task<IEnumerable<WellnessResponse>> GetHistoryAsync(long userId, int count = 30);
        Task<WellnessInsight> GetInsightAsync(long userId);
        Task DeleteResponsesAsync(long userId);
    }
}
