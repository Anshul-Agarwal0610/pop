using BackendAPI.Data;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using Dapper;

namespace BackendAPI.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IPollsRepository _pollsRepo;
        private readonly DapperContext _context;

        public DashboardRepository(IPollsRepository pollsRepo, DapperContext context)
        {
            _pollsRepo = pollsRepo;
            _context = context;
        }

        public async Task<DashboardData> GetDashboardDataAsync()
        {
            var trending = await _pollsRepo.GetTrendingAsync(5);
            var recent = await _pollsRepo.GetRecentAsync(5);

            using var conn = _context.CreateConnection();
            var stats = await conn.QueryFirstOrDefaultAsync<DashboardStats>(
                @"SELECT
                    COUNT(*) AS TotalPolls,
                    COALESCE(SUM(TotalVotes), 0) AS TotalVotes,
                    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActivePolls
                  FROM Polls"
            ) ?? new DashboardStats();

            return new DashboardData
            {
                TrendingPolls = trending.ToList(),
                RecentPolls = recent.ToList(),
                Stats = stats
            };
        }
    }
}
