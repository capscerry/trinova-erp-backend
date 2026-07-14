using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryDashboardController : ControllerBase
    {
        private readonly InventoryDashboardUsecase _usecase;

        public InventoryDashboardController(
            InventoryDashboardUsecase usecase)
        {
            _usecase = usecase;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await _usecase.GetDashboard();

            return Ok(result);
        }
    }
}