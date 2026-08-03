using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryDashboardController : ControllerBase
    {
        private readonly InventoryDashboardUsecase _usecase;
        private readonly ILogger<InventoryDashboardController> _logger;

        public InventoryDashboardController(
            InventoryDashboardUsecase usecase,
            ILogger<InventoryDashboardController> logger)
        {
            _usecase = usecase;
            _logger  = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _usecase.GetDashboard();
                return Ok(result);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,
                    "Database error in InventoryDashboard GET. SqlErrorNumber={Number}",
                    ex.Number);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error   = "Database error while loading inventory dashboard.",
                    code    = "DB_ERROR",
                    traceId = HttpContext.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in InventoryDashboard GET");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error   = "An unexpected error occurred while loading inventory dashboard.",
                    code    = "INTERNAL_ERROR",
                    traceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}
