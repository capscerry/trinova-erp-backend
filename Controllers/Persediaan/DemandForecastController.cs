using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(
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

        // Realtime Forecast (langsung dari AI)
        [HttpGet]
        public async Task<IActionResult> GetForecast()
        {
            var result = await _usecase.GetRealtimeForecast();
            return Ok(result);
        }

        // Generate Forecast Bulanan + Simpan ke forecast_history
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateMonthlyForecast()
        {
            var result = await _usecase.GenerateMonthlyForecast();
            return Ok(result);
        }

        // Ambil forecast terakhir dari forecast_history
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestForecast()
        {
            var result = await _usecase.GetLatestMonthlyForecast();
            return Ok(result);
        }

        // Download forecast terakhir ke Excel
        [HttpGet("download")]
        public async Task<IActionResult> DownloadForecast()
        {
            var file = await _usecase.DownloadForecast();

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DemandForecast_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }

    }
}