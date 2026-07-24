using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchasingDashboardUsecase
    {
        Task<object> GetDashboard();
        Task<SupplierRecommendationResult> GetRecommendation();
    }

    public class PurchasingDashboardUsecase : IPurchasingDashboardUsecase
    {
        private readonly IPurchasingDashboardRepo _purchasingDashboardRepo;
        private readonly ISupplierRiskUsecase     _supplierRiskUsecase;

        public PurchasingDashboardUsecase(
            IPurchasingDashboardRepo purchasingDashboardRepo,
            ISupplierRiskUsecase     supplierRiskUsecase)
        {
            _purchasingDashboardRepo = purchasingDashboardRepo;
            _supplierRiskUsecase     = supplierRiskUsecase;
        }

        /// <summary>
        /// Returns the full dashboard dataset.
        /// ERP KPIs come from the repo; supplier recommendation data comes from
        /// the single AHP-TOPSIS evaluation so every screen sees the same result.
        /// </summary>
        public async Task<object> GetDashboard()
        {
            // Run ERP stats and AHP-TOPSIS recommendation concurrently
            var erpStatsTask        = _purchasingDashboardRepo.GetDashboard();
            var recommendationTask  = _supplierRiskUsecase.GetRecommendation();

            await Task.WhenAll(erpStatsTask, recommendationTask);

            var erpStats       = erpStatsTask.Result;
            var recommendation = recommendationTask.Result;

            // Combine into a single response object so the frontend has everything
            // in one call without performing any additional calculations.
            return new
            {
                erp_stats      = erpStats,
                recommendation = recommendation
            };
        }

        /// <summary>
        /// Exposes the unified AHP-TOPSIS recommendation result directly.
        /// Used by GET /api/purchasing/dashboard/recommendation so the PO modal
        /// and recommendation page can fetch the same dataset without a full dashboard load.
        /// </summary>
        public async Task<SupplierRecommendationResult> GetRecommendation()
        {
            return await _supplierRiskUsecase.GetRecommendation();
        }
    }
}
