using System.Net.Http.Json;
using System.Text.Json;

using PersediaanForecastRequest =
    trinova_erp_backend.Models.Persediaan.ForecastRequest;

using ForecastResult =
    trinova_erp_backend.Models.Persediaan.ForecastResult;

using AIModelComparisonResponse =
    trinova_erp_backend.Models.AI.ModelComparisonResponse;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class ForecastClient
    {
        private readonly HttpClient _httpClient;

        public ForecastClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // =====================================================================
        // REALTIME FORECAST
        // =====================================================================

        public async Task<List<ForecastResult>> GetRealtimeForecast(
            PersediaanForecastRequest request
        )
        {
            Console.WriteLine("===== REQUEST =====");

            Console.WriteLine(
                JsonSerializer.Serialize(
                    request,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                )
            );

            Console.WriteLine("===================");

            var response =
                await _httpClient.PostAsJsonAsync(
                    "/forecast",
                    request
                );

            if (!response.IsSuccessStatusCode)
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                throw new Exception(
                    $"FastAPI Error {(int)response.StatusCode}: {error}"
                );
            }

            var result =
                await response.Content.ReadFromJsonAsync<
                    List<ForecastResult>
                >();

            return result ?? new List<ForecastResult>();
        }


        // =====================================================================
        // MONTHLY FORECAST
        // =====================================================================

        public async Task<List<ForecastResult>> GenerateMonthlyForecast(
            PersediaanForecastRequest request
        )
        {
            Console.WriteLine("===== REQUEST =====");

            Console.WriteLine(
                JsonSerializer.Serialize(
                    request,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                )
            );

            Console.WriteLine("===================");

            var response =
                await _httpClient.PostAsJsonAsync(
                    "/forecast/monthly/generate",
                    request
                );

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<
                    List<ForecastResult>
                >();

            return result ?? new List<ForecastResult>();
        }


        // =====================================================================
        // MODEL COMPARISON
        // =====================================================================

        public async Task<trinova_erp_backend.Models.AI.ModelComparisonResponse> CompareModels(
                PersediaanForecastRequest request
            )
            {
                Console.WriteLine("===== MODEL COMPARISON REQUEST =====");

                Console.WriteLine(
                    JsonSerializer.Serialize(
                        request,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }
                    )
                );

                Console.WriteLine("====================================");

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "/model-comparison",
                        request
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    throw new Exception(
                        $"FastAPI Error {(int)response.StatusCode}: {error}"
                    );
                }

                var json =
                    await response.Content.ReadAsStringAsync();

                Console.WriteLine(
                    "===== FASTAPI MODEL COMPARISON RAW RESPONSE ====="
                );

                Console.WriteLine(json);

                Console.WriteLine(
                    "================================================="
                );

                var result =
                    JsonSerializer.Deserialize<AIModelComparisonResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                if (result is null)
                {
                    throw new Exception(
                        "FastAPI returned an empty model comparison response."
                    );
                }

                Console.WriteLine(
                    "===== DESERIALIZED MODEL COMPARISON ====="
                );

                Console.WriteLine(
                    $"EvaluationMethod: {result.EvaluationMethod}"
                );

                Console.WriteLine(
                    $"TrainingPeriod: {result.TrainingPeriod}"
                );

                Console.WriteLine(
                    $"TestingPeriod: {result.TestingPeriod}"
                );

                Console.WriteLine(
                    $"TotalProducts: {result.TotalProducts}"
                );

                Console.WriteLine(
                    $"ProductsEvaluated: {result.ProductsEvaluated}"
                );

                Console.WriteLine(
                    $"BestModel: {result.BestModel}"
                );

                Console.WriteLine(
                    $"Models Count: {result.Models.Count}"
                );

                Console.WriteLine(
                    "=========================================="
                );

                return result;
            }
    }
}