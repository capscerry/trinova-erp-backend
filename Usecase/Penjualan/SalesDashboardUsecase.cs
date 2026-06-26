using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface ISalesDashboardUsecase
    {
        Task<SalesDashboard> GetDashboard();
    }

    public class SalesDashboardUsecase : ISalesDashboardUsecase
    {
        private readonly ISalesDashboardRepo _salesDashboardRepo;

        public SalesDashboardUsecase(ISalesDashboardRepo salesDashboardRepo)
        {
            _salesDashboardRepo = salesDashboardRepo;
        }

        public async Task<SalesDashboard> GetDashboard()
        {
            return await _salesDashboardRepo.GetDashboard();
        }
    }
}
