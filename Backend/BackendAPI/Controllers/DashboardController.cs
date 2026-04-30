using BackendAPI.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardRepository _dashboardRepo;

        public DashboardController(IDashboardRepository dashboardRepo)
        {
            _dashboardRepo = dashboardRepo;
        }

        // GET /api/dashboard
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var data = await _dashboardRepo.GetDashboardDataAsync();
            return Ok(data);
        }
    }
}
