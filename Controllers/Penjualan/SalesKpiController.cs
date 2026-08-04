using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class SalesKpiController : ControllerBase
    {
        private readonly ISalesKpiUsecase _salesKpiUsecase;

        public SalesKpiController(ISalesKpiUsecase salesKpiUsecase)
        {
            _salesKpiUsecase = salesKpiUsecase;
        }

        // Sales executive-dashboard KPI cards, charts, funnel and leaderboard,
        // computed server-side in one shot. from/to/prevFrom/prevTo are
        // supplied by the frontend (it already knows what "last6m" etc. means);
        // this endpoint just aggregates whatever range it's given.
        [HttpGet("/api/sales-kpi")]
        public async Task<IActionResult> GetSalesKpi(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] DateTime prevFrom,
            [FromQuery] DateTime prevTo,
            [FromQuery] int? customerId,
            [FromQuery] string? category,
            [FromQuery] string? status)
        {
            try
            {
                var query = new SalesKpiQuery
                {
                    From = from,
                    To = to,
                    PrevFrom = prevFrom,
                    PrevTo = prevTo,
                    CustomerId = customerId,
                    Category = string.IsNullOrWhiteSpace(category) ? null : category,
                    Status = string.IsNullOrWhiteSpace(status) ? null : status,
                };

                var result = await _salesKpiUsecase.GetSalesKpiAsync(query);

                return Ok(new
                {
                    success = true,
                    message = "Sales KPI berhasil diambil.",
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
