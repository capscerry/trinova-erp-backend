using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchasingDashboardUsecase
    {
        Task<object> GetDashboard();
    }

    public class PurchasingDashboardUsecase : IPurchasingDashboardUsecase
    {
        private readonly IPurchasingDashboardRepo _purchasingDashboardRepo;

        public PurchasingDashboardUsecase(IPurchasingDashboardRepo purchasingDashboardRepo)
        {
            _purchasingDashboardRepo = purchasingDashboardRepo;
        }

        public async Task<object> GetDashboard()
        {
            var result = await _purchasingDashboardRepo.GetDashboard();

            return result;
        }
    }
}