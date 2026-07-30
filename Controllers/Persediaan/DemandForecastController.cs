using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class DemandForecastController : ControllerBase
    {
        private readonly DemandForecastUsecase _usecase;
        private readonly ILogger<DemandForecastController> _logger;

        public DemandForecastController(
            DemandForecastUsecase usecase,
            ILogger<DemandForecastController> logger)
        {
            _usecase = usecase;
            _logger  = logger;
        }

        // [HttpGet]
        // public async Task<IActionResult> GetForecast()
        // {
        //     var result = await _usecase.GetRealtimeForecast();

        [HttpGet]
        public async Task<IActionResult> GetForecast()
        {
            try
            {
                var result = await _usecase.GenerateForecast();
                return Ok(result);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,
                    "Database error in DemandForecast GET. SqlErrorNumber={Number}",
                    ex.Number);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error   = "Database error while generating demand forecast.",
                    code    = "DB_ERROR",
                    traceId = HttpContext.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in DemandForecast GET");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error   = "An unexpected error occurred while generating demand forecast.",
                    code    = "INTERNAL_ERROR",
                    traceId = HttpContext.TraceIdentifier
                });
            }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateMonthlyForecast()
        {
            await _usecase.GenerateMonthlyForecast();

            return Ok(new
            {
                message = "Monthly forecast generated successfully."
            });
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestForecast()
        {
            var result = await _usecase.GetLatestMonthlyForecast();

            return Ok(result);
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadForecast()
        {
            var fileBytes = await _usecase.DownloadForecast();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DemandForecast_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
    }
}
