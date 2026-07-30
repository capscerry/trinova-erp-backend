using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class DemandForecastController
        : ControllerBase
    {
        private readonly DemandForecastUsecase
            _usecase;

        public DemandForecastController(
            DemandForecastUsecase usecase
        )
        {
            _usecase = usecase;
        }

        [HttpGet]
        public async Task<IActionResult> GetForecast()
        {
            var result = await _usecase.GetRealtimeForecast();

            return Ok(result);
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