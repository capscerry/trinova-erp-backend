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
        private readonly ForecastClient     _forecastClient;
        private readonly ForecastDatasetRepo _datasetRepo;

        public DemandForecastUsecase(
            ForecastClient      forecastClient,
            ForecastDatasetRepo datasetRepo)
        {
            _forecastClient = forecastClient;
            _datasetRepo    = datasetRepo;
        }

        /// <summary>
        /// Builds the forecast dataset from SQL Server and sends it to the AI.
        /// Returns an empty list when the dataset is empty or the AI is unavailable.
        /// Never throws — failures are handled inside ForecastClient.
        /// </summary>
        public async Task<List<ForecastResult>> GenerateForecast()
        {
            var dataset = await _datasetRepo.GetForecastDatasetAsync();
            return await _forecastClient.PostForecast(dataset);
        }
    }
}
