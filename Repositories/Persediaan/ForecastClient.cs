using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using trinova_erp_backend.Models.AI;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    /// <summary>
    /// Low-level HTTP client for the Railway Inventory AI service.
    /// Used internally by DemandForecastUsecase and InventoryDashboardUsecase.
    ///
    /// Updated API contract:
    ///   POST /forecast — body: ForecastRequest { items: [...] }
    ///                  — response: ForecastResult[]
    ///
    /// The caller must supply the dataset (List&lt;ForecastDatasetItem&gt;) which is
    /// built from ForecastDatasetRepo before this method is invoked.
    ///
    /// CRITICAL: DemandForecastUsecase and InventoryDashboardUsecase depend on this
    /// class — their signatures must never change.  This class wraps all HTTP calls
    /// with full exception handling so that Inventory business logic always receives
    /// either a valid list or an empty list, and never throws.
    ///
    /// The IInventoryAIService (Services/InventoryAI/) handles the public-facing
    /// API endpoints (/api/inventory-ai/*) independently of this class.
    /// </summary>
    public class ForecastClient
    {
        private readonly HttpClient              _httpClient;
        private readonly ILogger<ForecastClient> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Retry delays for PostForecast: 1 s → 2 s → 4 s
        private static readonly TimeSpan[] _retryDelays =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        ];

        public ForecastClient(HttpClient httpClient, ILogger<ForecastClient> logger)
        {
            _httpClient = httpClient;
            _logger     = logger;
        }

        public async Task<List<ForecastResult>> GetRealtimeForecast()
        {
            if (dataset is null || dataset.Count == 0)
            {
                _logger.LogWarning(
                    "Inventory AI (ForecastClient): PostForecast called with empty dataset — skipping AI call.");
                return new List<ForecastResult>();
            }

            var payload = new ForecastRequest { Items = dataset };
            var sw      = Stopwatch.StartNew();

            for (int attempt = 0; attempt <= _retryDelays.Length; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Inventory AI (ForecastClient) → POST /forecast (attempt {Attempt}), {Count} rows",
                        attempt + 1, dataset.Count);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    var response = await _httpClient.PostAsJsonAsync(
                        "/forecast", payload, _jsonOpts, cts.Token);

                    sw.Stop();
                    _logger.LogInformation(
                        "Inventory AI (ForecastClient) ← POST /forecast | Status: {Status} | Elapsed: {Elapsed} ms",
                        (int)response.StatusCode, sw.ElapsedMilliseconds);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Inventory AI (ForecastClient): POST /forecast returned HTTP {Status}. Elapsed: {Elapsed} ms",
                            (int)response.StatusCode, sw.ElapsedMilliseconds);

                        // 5xx — retry; 4xx — don't retry
                        if ((int)response.StatusCode >= 500 && attempt < _retryDelays.Length)
                        {
                            await Task.Delay(_retryDelays[attempt]);
                            sw.Restart();
                            continue;
                        }

                        return new List<ForecastResult>();
                    }

                    var json   = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<ForecastResult>>(json, _jsonOpts);

                    return result ?? new List<ForecastResult>();
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "Inventory AI (ForecastClient): POST /forecast timed out after {Elapsed} ms. Attempt {Attempt}/{Max}",
                        sw.ElapsedMilliseconds, attempt + 1, _retryDelays.Length + 1);

                    if (attempt < _retryDelays.Length)
                    {
                        await Task.Delay(_retryDelays[attempt]);
                        sw.Restart();
                        continue;
                    }

                    return new List<ForecastResult>();
                }
                catch (HttpRequestException ex)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        ex,
                        "Inventory AI (ForecastClient): connectivity error on POST /forecast. Attempt {Attempt}/{Max}. Elapsed: {Elapsed} ms",
                        attempt + 1, _retryDelays.Length + 1, sw.ElapsedMilliseconds);

                    if (attempt < _retryDelays.Length)
                    {
                        await Task.Delay(_retryDelays[attempt]);
                        sw.Restart();
                        continue;
                    }

                    return new List<ForecastResult>();
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(
                        ex,
                        "Inventory AI (ForecastClient): unexpected error on POST /forecast. Elapsed: {Elapsed} ms",
                        sw.ElapsedMilliseconds);

                    // Non-transient — do not retry
                    return new List<ForecastResult>();
                }
            }

            return new List<ForecastResult>();
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
