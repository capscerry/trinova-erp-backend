using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult>
            GetForecast()
        {
            var result =
                await _usecase.GenerateForecast();

            return Ok(result);
        }
    }
}