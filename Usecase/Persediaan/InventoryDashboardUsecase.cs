using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class InventoryDashboardUsecase
    {
        private readonly InventoryStockRepo _stockRepo;
        private readonly ForecastClient _forecastClient;

        private readonly MasterProductRepo _productRepo;

        public InventoryDashboardUsecase(
            InventoryStockRepo stockRepo,
            ForecastClient forecastClient,
            MasterProductRepo productRepo)
        {
            _stockRepo = stockRepo;
            _forecastClient = forecastClient;
            _productRepo = productRepo;
        }

        public async Task<InventoryDashboard> GetDashboard()
        {
            var stocks = await _stockRepo.GetAllAsync();
            var forecasts = await _forecastClient.GetForecast();

            var dashboard = new InventoryDashboard();

            dashboard.TotalProducts = await _productRepo.GetTotalProductsAsync();

            dashboard.TotalStock = stocks.Sum(x => x.qty_on_hand);

            foreach (var forecast in forecasts)
            {
                var stock = stocks
                    .Where(x => x.product_id == forecast.ProductId)
                    .Sum(x => x.qty_on_hand);

                if (stock >= (decimal)forecast.ForecastNextMonth)
                    dashboard.SafeStock++;
                else
                    dashboard.CriticalStock++;
            }

            return dashboard;
        }
    }
}