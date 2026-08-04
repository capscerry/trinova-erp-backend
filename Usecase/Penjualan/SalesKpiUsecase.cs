using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface ISalesKpiUsecase
    {
        Task<SalesKpiResult> GetSalesKpiAsync(SalesKpiQuery query);
    }

    public class SalesKpiUsecase : ISalesKpiUsecase
    {
        private readonly ISalesKpiRepo _salesKpiRepo;

        public SalesKpiUsecase(ISalesKpiRepo salesKpiRepo)
        {
            _salesKpiRepo = salesKpiRepo;
        }

        public async Task<SalesKpiResult> GetSalesKpiAsync(SalesKpiQuery query)
        {
            return await _salesKpiRepo.GetSalesKpiAsync(query);
        }
    }
}
