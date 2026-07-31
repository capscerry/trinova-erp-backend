using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    /// <summary>
    /// Orchestrates demand forecast generation:
    ///   1. Fetch the stock-usage dataset from SQL Server via ForecastDatasetRepo.
    ///   2. POST the dataset to the Inventory AI service via ForecastClient.
    ///   3. Return the ForecastResult list to the controller.
    ///
    /// The AI service is called with a POST /forecast payload so it always trains
    /// on fresh SQL Server data rather than reading the database directly.
    /// </summary>
    public class DemandForecastUsecase
    {
        private readonly ForecastClient _forecastClient;
        private readonly ForecastExcelExporter _excelExporter;

        public DemandForecastUsecase(
            ForecastClient forecastClient,
            ForecastExcelExporter excelExporter
        )
        {
            _forecastClient = forecastClient;
            _excelExporter = excelExporter;
        }

        public async Task<List<ForecastResult>> GetRealtimeForecast()
        {
            return await _forecastClient.GetRealtimeForecast();
        }

        public async Task GenerateMonthlyForecast()
        {
            await _forecastClient.GenerateMonthlyForecast();
        }

        public async Task<List<ForecastResult>> GetLatestMonthlyForecast()
        {
            return await _forecastClient.GetLatestMonthlyForecast();
        }

        public async Task<byte[]> DownloadForecast()
        {
            var forecasts = await _forecastClient.GetLatestMonthlyForecast();

            return _excelExporter.Export(forecasts);
        }

        // ── New forecast endpoints ────────────────────────────────────────────

        public async Task<List<ForecastResult>> GetRealtimeForecast()
        {
            return await _forecastClient.GetRealtimeForecast();
        }

        public async Task GenerateMonthlyForecast()
        {
            await _forecastClient.GenerateMonthlyForecast();
        }

        public async Task<List<ForecastResult>> GetLatestMonthlyForecast()
        {
            return await _forecastClient.GetLatestMonthlyForecast();
        }

        public async Task<byte[]> DownloadForecast()
        {
            var forecasts = await _forecastClient.GetLatestMonthlyForecast();
            return _excelExporter.Export(forecasts);
        }
    }
}
