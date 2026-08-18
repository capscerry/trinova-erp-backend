using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;
using Microsoft.AspNetCore.Http.Timeouts;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(
        Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan"
    )]
    public class DemandForecastController : ControllerBase
    {
        private readonly DemandForecastUsecase _usecase;

        public DemandForecastController(
            DemandForecastUsecase usecase
        )
        {
            _usecase = usecase;
        }


        // =====================================================================
        // REALTIME FORECAST
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> GetForecast()
        {
            var result =
                await _usecase.GetRealtimeForecast();

            return Ok(result);
        }


        // =====================================================================
        // GENERATE MONTHLY FORECAST
        // =====================================================================

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateMonthlyForecast()
        {
            var result =
                await _usecase.GenerateMonthlyForecast();

            return Ok(result);
        }


        // =====================================================================
        // LATEST MONTHLY FORECAST
        // =====================================================================

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestForecast()
        {
            var result =
                await _usecase.GetLatestMonthlyForecast();

            return Ok(result);
        }


        // =====================================================================
        // DOWNLOAD FORECAST
        // =====================================================================

        [HttpGet("download")]
        public async Task<IActionResult> DownloadForecast()
        {
            var file =
                await _usecase.DownloadForecast();

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DemandForecast_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }


        // =====================================================================
        // AI MODEL COMPARISON
        // =====================================================================

        [HttpGet("model-comparison")]
        [RequestTimeout("model-comparison")]
        public async Task<IActionResult> GetModelComparison()
        {
            var result = await _usecase.GetModelComparison();

            return Ok(result);
        }
    }
}