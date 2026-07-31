using System.Text.Json;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class ForecastClient
    {
        private readonly HttpClient _httpClient;

        public ForecastClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ForecastResult>> GetRealtimeForecast()
        {
            var response = await _httpClient.GetAsync("/forecast");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<List<ForecastResult>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result ?? new List<ForecastResult>();
        }
        public async Task GenerateMonthlyForecast()
        {
            var response = await _httpClient.PostAsync(
                "/forecast/monthly/generate",
                null
            );

            response.EnsureSuccessStatusCode();
        }

        public async Task<List<ForecastResult>> GetLatestMonthlyForecast()
        {
            var response = await _httpClient.GetAsync(
                "/forecast/monthly/latest"
            );

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<List<ForecastResult>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result ?? new List<ForecastResult>();
        }

    }
}