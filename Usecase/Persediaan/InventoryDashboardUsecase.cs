using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class InventoryDashboardUsecase
    {
        private readonly InventoryStockRepo  _stockRepo;
        private readonly ForecastClient      _forecastClient;
        private readonly MasterProductRepo   _productRepo;
        private readonly ForecastDatasetRepo _datasetRepo;

        public InventoryDashboardUsecase(
            InventoryStockRepo  stockRepo,
            ForecastClient      forecastClient,
            MasterProductRepo   productRepo,
            ForecastDatasetRepo datasetRepo)
        {
            _stockRepo      = stockRepo;
            _forecastClient = forecastClient;
            _productRepo    = productRepo;
            _datasetRepo    = datasetRepo;
        }

        public async Task<InventoryDashboard> GetDashboard()
        {
            var stocks    = await _stockRepo.GetAllAsync();
            var forecasts = await _forecastClient.GetRealtimeForecast();

            if (!forecasts.Any())
            {
                return new InventoryDashboard
                {
                    TotalProducts = await _productRepo.GetTotalProductsAsync(),
                    TotalStock = stocks.Sum(x => x.qty_on_hand)
                };
            }

            var highestForecast = forecasts
                .OrderByDescending(x => x.ForecastNextMonth)
                .First();

            var lowestForecast = forecasts
                .OrderBy(x => x.ForecastNextMonth)
                .First();

            var averageForecast = forecasts.Average(x => x.ForecastNextMonth);
            var forecastedProducts = forecasts.Count;

            var topForecastProducts = forecasts
                .OrderByDescending(x => x.ForecastNextMonth)
                .Take(5)
                .Select(x => new ForecastProduct
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Forecast = Math.Round((decimal)x.ForecastNextMonth, 2)
                })
                .ToList();

            var dashboard = new InventoryDashboard
            {
                TotalProducts = await _productRepo.GetTotalProductsAsync(),
                TotalStock = stocks.Sum(x => x.qty_on_hand)
            };

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

            dashboard.AiSummary = new InventoryAiSummary
            {
                ForecastMonth = highestForecast.ForecastMonth,

                GeneratedAt = DateTime.Parse(highestForecast.GeneratedAt),

                ForecastedProducts = forecastedProducts,

                AverageForecast = Math.Round((decimal)averageForecast, 2),

                NeedRestock = dashboard.CriticalStock,

                TopForecastProducts = topForecastProducts,

                HighestDemand = new ForecastProduct
                {
                    ProductId = highestForecast.ProductId,
                    ProductName = highestForecast.ProductName,
                    Forecast = Math.Round((decimal)highestForecast.ForecastNextMonth, 2)
                },

                LowestDemand = new ForecastProduct
                {
                    ProductId = lowestForecast.ProductId,
                    ProductName = lowestForecast.ProductName,
                    Forecast = Math.Round((decimal)lowestForecast.ForecastNextMonth, 2)
                }
            };

            return dashboard;
        }
    }
}