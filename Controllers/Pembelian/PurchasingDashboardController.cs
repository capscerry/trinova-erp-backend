using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/purchasing/dashboard")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
    public class PurchasingDashboardController : ControllerBase
    {
        private readonly IPurchasingDashboardUsecase _purchasingDashboardUsecase;

        public PurchasingDashboardController(
            IPurchasingDashboardUsecase purchasingDashboardUsecase
        )
        {
            _purchasingDashboardUsecase = purchasingDashboardUsecase;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _purchasingDashboardUsecase
                .GetDashboard();

            return Ok(new
            {
                status = true,
                data = result
            });
        }
    }
}
