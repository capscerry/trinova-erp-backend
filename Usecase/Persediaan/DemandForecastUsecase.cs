using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
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
            _forecastDatasetRepository = forecastDatasetRepository;
            _forecastHistoryRepository = forecastHistoryRepository;
            _forecastClient = forecastClient;
            _excelExporter = excelExporter;
        }

        public async Task<List<ForecastResult>> GetRealtimeForecast()
        {
            var dataset = await _forecastDatasetRepository.GetForecastDataset();

            var request = new ForecastRequest
            {
                Items = dataset
            };

            return await _forecastClient.GetRealtimeForecast(request);
        }

        public async Task<List<ForecastResult>> GenerateMonthlyForecast()
        {
            var dataset = await _forecastDatasetRepository.GetForecastDataset();

            var request = new ForecastRequest
            {
                Items = dataset
            };

            var forecasts =
                await _forecastClient.GenerateMonthlyForecast(request);

            await _forecastHistoryRepository.ReplaceForecast(forecasts);

            return forecasts;
        }

        public async Task<List<ForecastResult>> GetLatestMonthlyForecast()
        {
            return await _forecastHistoryRepository.GetLatestForecast();
        }

        public async Task<byte[]> DownloadForecast()
        {
            var forecasts =
                await _forecastHistoryRepository.GetLatestForecast();

            return _excelExporter.Export(forecasts);
        }
    }
}