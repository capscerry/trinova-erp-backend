using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class SalesDashboardController : ControllerBase
    {
        private readonly ISalesDashboardUsecase _salesDashboardUsecase;

        public SalesDashboardController(ISalesDashboardUsecase salesDashboardUsecase)
        {
            _salesDashboardUsecase = salesDashboardUsecase;
        }

        [HttpGet("/api/sales-dashboard")]
        public async Task<IActionResult> GetDashboard() 
        {
            try
            {
                var result = await _salesDashboardUsecase.GetDashboard();
                return Ok(new
                {
                    success = true,
                    message = "Data dashboard penjualan berhasil diambil.",
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
