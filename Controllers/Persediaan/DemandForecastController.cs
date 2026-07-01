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
        public async Task<IActionResult>
            GetForecast()
        {
            var result =
                await _usecase.GenerateForecast();

            return Ok(result);
        }
    }
}
