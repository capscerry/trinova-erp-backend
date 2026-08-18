using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    /// <summary>
    /// Orchestrates demand forecast generation and AI model comparison:
    ///
    /// Forecast:
    ///   1. Fetch the stock-usage dataset from SQL Server.
    ///   2. POST the dataset to the Inventory AI service.
    ///   3. Return the forecast result.
    ///
    /// Model Comparison:
    ///   1. Fetch the same stock-usage dataset from SQL Server.
    ///   2. POST the dataset to the Inventory AI service.
    ///   3. Compare Linear Regression and XGBoost.
    ///   4. Return MAE, RMSE, R² and the best model.
    /// </summary>
    public class DemandForecastUsecase
    {
        private readonly ForecastDatasetRepository _forecastDatasetRepository;
        private readonly ForecastHistoryRepository _forecastHistoryRepository;
        private readonly ForecastClient _forecastClient;
        private readonly ForecastExcelExporter _excelExporter;

        public DemandForecastUsecase(
            ForecastDatasetRepository forecastDatasetRepository,
            ForecastHistoryRepository forecastHistoryRepository,
            ForecastClient forecastClient,
            ForecastExcelExporter excelExporter
        )
        {
            _forecastDatasetRepository =
                forecastDatasetRepository;

            _forecastHistoryRepository =
                forecastHistoryRepository;

            _forecastClient =
                forecastClient;

            _excelExporter =
                excelExporter;
        }


        // =====================================================================
        // REALTIME FORECAST
        // =====================================================================

        public async Task<List<ForecastResult>> GetRealtimeForecast()
        {
            var dataset =
                await _forecastDatasetRepository.GetForecastDataset();

            var request = new ForecastRequest
            {
                Items = dataset
            };

            return await _forecastClient.GetRealtimeForecast(
                request
            );
        }


        // =====================================================================
        // MONTHLY FORECAST
        // =====================================================================

        public async Task<List<ForecastResult>> GenerateMonthlyForecast()
        {
            var dataset =
                await _forecastDatasetRepository.GetForecastDataset();

            var request = new ForecastRequest
            {
                Items = dataset
            };

            var forecasts =
                await _forecastClient.GenerateMonthlyForecast(
                    request
                );

            await _forecastHistoryRepository.ReplaceForecast(
                forecasts
            );

            return forecasts;
        }


        // =====================================================================
        // LATEST MONTHLY FORECAST
        // =====================================================================

        public async Task<List<ForecastResult>> GetLatestMonthlyForecast()
        {
            return await _forecastHistoryRepository
                .GetLatestForecast();
        }


        // =====================================================================
        // DOWNLOAD FORECAST
        // =====================================================================

        public async Task<byte[]> DownloadForecast()
        {
            var forecasts =
                await _forecastHistoryRepository
                    .GetLatestForecast();

            return _excelExporter.Export(
                forecasts
            );
        }


        // =====================================================================
        // MODEL COMPARISON
        // =====================================================================

        public async Task<trinova_erp_backend.Models.AI.ModelComparisonResponse> GetModelComparison()
        {
            var dataset =
                await _forecastDatasetRepository
                    .GetForecastDataset();

            var request = new ForecastRequest
            {
                Items = dataset
            };

            return await _forecastClient.CompareModels(
                request
            );
        }
    }
}