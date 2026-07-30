using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
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
    }
}