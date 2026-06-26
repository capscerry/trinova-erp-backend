using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class DemandForecastUsecase
    {
        private readonly ForecastClient _forecastClient;
        public DemandForecastUsecase(
            ForecastClient forecastClient
        )
        {
            _forecastClient = forecastClient;
        }

        public async Task<List<ForecastResult>> GenerateForecast()
        {
            return await _forecastClient.GetForecast();
        }
    }
}