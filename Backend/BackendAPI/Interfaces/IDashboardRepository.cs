using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardData> GetDashboardDataAsync();
    }
}
