using System.Net.Http.Json;
using trinova_erp_backend.Models.Persediaan;
using System.Text.Json;


namespace trinova_erp_backend.Repositories.Persediaan
{
    public class ForecastClient
    {
        private readonly HttpClient _httpClient;

        public ForecastClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ForecastResult>> GetRealtimeForecast(
            ForecastRequest request
        )
        {
            Console.WriteLine("===== REQUEST =====");
            Console.WriteLine(JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            Console.WriteLine("===================");

            var response =
                await _httpClient.PostAsJsonAsync("/forecast", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(
                    $"FastAPI Error {(int)response.StatusCode}: {error}"
                );
            }

            var result = await response.Content.ReadFromJsonAsync<
                List<ForecastResult>
            >();

            return result ?? new List<ForecastResult>();
        }

        public async Task<List<ForecastResult>> GenerateMonthlyForecast(
            ForecastRequest request
        )
        {
            Console.WriteLine("===== REQUEST =====");
            Console.WriteLine(JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            Console.WriteLine("===================");

            var response = await _httpClient.PostAsJsonAsync(
                "/forecast/monthly/generate",
                request
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<
                List<ForecastResult>
            >();

            return result ?? new List<ForecastResult>();
        }
    }
}