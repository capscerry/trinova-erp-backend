using System.Diagnostics;
using System.Text.Json;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    /// <summary>
    /// Low-level HTTP client for the Railway Inventory AI service.
    /// Used internally by DemandForecastUsecase and InventoryDashboardUsecase.
    ///
    /// CRITICAL: DemandForecastUsecase and InventoryDashboardUsecase depend on this
    /// class — their signatures must never change.  This class wraps all HTTP calls
    /// with full exception handling so that Inventory business logic always receives
    /// either a valid list or an empty list, and never throws.
    ///
    /// The new IInventoryAIService (Services/InventoryAI/) handles the public-facing
    /// API endpoints (/api/inventory-ai/*) independently of this class.
    /// </summary>
    public class ForecastClient
    {
        private readonly HttpClient                 _httpClient;
        private readonly ILogger<ForecastClient>    _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Retry delays for GetForecast: 1 s → 2 s → 4 s
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

        /// <summary>
        /// Fetches the demand forecast from GET /forecast.
        ///
        /// Returns an empty list when the AI service is unavailable — this keeps
        /// InventoryDashboardUsecase and DemandForecastUsecase working normally.
        /// Never throws; all failures are logged as warnings.
        ///
        /// Retry policy: exponential backoff, up to 3 retries (1 s, 2 s, 4 s).
        /// Timeout: 30 seconds per attempt.
        /// </summary>
        public async Task<List<ForecastResult>> GetForecast()
        {
            var sw = Stopwatch.StartNew();

            for (int attempt = 0; attempt <= _retryDelays.Length; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Inventory AI (ForecastClient) → GET /forecast (attempt {Attempt})",
                        attempt + 1);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    var response = await _httpClient.GetAsync("/forecast", cts.Token);

                    sw.Stop();
                    _logger.LogInformation(
                        "Inventory AI (ForecastClient) ← /forecast | Status: {Status} | Elapsed: {Elapsed} ms",
                        (int)response.StatusCode, sw.ElapsedMilliseconds);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Inventory AI (ForecastClient): /forecast returned HTTP {Status}. Elapsed: {Elapsed} ms",
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

                    var json = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<List<ForecastResult>>(json, _jsonOpts);

                    return result ?? new List<ForecastResult>();
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "Inventory AI (ForecastClient): /forecast timed out after {Elapsed} ms. Attempt {Attempt}/{Max}",
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
                        "Inventory AI (ForecastClient): connectivity error on /forecast. Attempt {Attempt}/{Max}. Elapsed: {Elapsed} ms",
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
                        "Inventory AI (ForecastClient): unexpected error on /forecast. Elapsed: {Elapsed} ms",
                        sw.ElapsedMilliseconds);

                    // Non-transient — do not retry
                    return new List<ForecastResult>();
                }
            }

            return new List<ForecastResult>();
        }
    }
}
