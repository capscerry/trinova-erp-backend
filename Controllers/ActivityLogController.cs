using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Repositories;

namespace trinova_erp_backend.Controllers
{
    [ApiController]
    [Authorize]
    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogRepo _activityLogRepo;

        public ActivityLogController(IActivityLogRepo activityLogRepo)
        {
            _activityLogRepo = activityLogRepo;
        }

        // Cross-module "Aktivitas Terkini" dashboard timeline (Purchasing, Sales,
        // Inventory, etc.) -- any authenticated role can read it; the frontend
        // decides how much of it to show per dashboard.
        [HttpGet("/api/activity-log")]
        public async Task<IActionResult> GetRecent([FromQuery] int limit = 20)
        {
            try
            {
                var result = await _activityLogRepo.GetRecentAsync(Math.Clamp(limit, 1, 100));

                return Ok(new
                {
                    success = true,
                    message = "Activity log berhasil diambil.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }
    }
}
