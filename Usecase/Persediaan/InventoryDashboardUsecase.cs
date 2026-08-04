using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Services.InventoryAI;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class InventoryDashboardUsecase
    {
        private readonly InventoryStockRepo _stockRepo;
        private readonly IInventoryAIService _inventoryAIService;
        private readonly MasterProductRepo _productRepo;

        public InventoryDashboardUsecase(
            InventoryStockRepo stockRepo,
            IInventoryAIService inventoryAIService,
            MasterProductRepo productRepo)
        {
            _stockRepo = stockRepo;
            _inventoryAIService = inventoryAIService;
            _productRepo = productRepo;
        }

        public async Task<InventoryDashboard> GetDashboard()
        {
            var stocks = await _stockRepo.GetAllAsync();

            // IInventoryAIService never throws -- it degrades gracefully and
            // reports failure via `success = false`.
            var forecastResult = await _inventoryAIService.GetForecastAsync();

            if (!forecastResult.success || forecastResult.data is null || forecastResult.data.Count == 0)
            {
                return new InventoryDashboard
                {
                    TotalProducts = await _productRepo.GetTotalProductsAsync(),
                    TotalStock = stocks.Sum(x => x.qty_on_hand)
                };
            }

            var forecasts = forecastResult.data;

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
                GeneratedAt = highestForecast.GeneratedAt,
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