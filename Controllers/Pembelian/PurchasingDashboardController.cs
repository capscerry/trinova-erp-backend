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

        /// <summary>
        /// Full purchasing dashboard.
        /// Response contains two top-level keys:
        ///   - erp_stats      : KPI counters, PO status summary, top supplier by volume
        ///   - recommendation : unified AHP-TOPSIS result (ranking + profiles)
        ///
        /// The recommendation section is the same dataset served by
        /// GET /api/supplier-risk/recommendation and GET /api/purchasing/dashboard/recommendation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var result = await _purchasingDashboardUsecase.GetDashboard();
                return Ok(new { status = true, data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lightweight endpoint that returns only the unified AHP-TOPSIS
        /// recommendation result — identical to what appears in the dashboard's
        /// recommendation section.
        ///
        /// Consume this endpoint from:
        ///   - Supplier Recommendation page
        ///   - Purchase Order supplier recommendation modal
        ///   - Any recommendation cards / buttons
        ///
        /// Never re-rank, re-score, or re-sort this data on the frontend.
        /// Display supplier names, topsis_score, topsis_rank, and profiles exactly
        /// as returned.
        /// </summary>
        [HttpGet("recommendation")]
        public async Task<IActionResult> GetRecommendation()
        {
            try
            {
                var result = await _purchasingDashboardUsecase.GetRecommendation();
                return Ok(new { status = true, message = "Supplier recommendation successful", data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
    }
}
