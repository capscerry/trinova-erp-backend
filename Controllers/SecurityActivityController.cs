using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Repositories;

namespace trinova_erp_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin,admin")]
    public class SecurityActivityController : ControllerBase
    {
        private readonly IActivityLogRepo _activityLogRepo;

        public SecurityActivityController(IActivityLogRepo activityLogRepo)
        {
            _activityLogRepo = activityLogRepo;
        }

        [HttpGet("/api/security-activity")]
        public async Task<IActionResult> GetSecurityActivity([FromQuery] int take = 12)
        {
            try
            {
                var result = await _activityLogRepo.GetSecurityAlertsAsync(Math.Clamp(take, 1, 50));

                return Ok(new
                {
                    success = true,
                    message = "Security activity berhasil diambil.",
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
