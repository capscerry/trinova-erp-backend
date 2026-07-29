using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using trinova_erp_backend.Models.AI;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Services.InventoryAI
{
    /// <summary>
    /// Handles all HTTP communication between the ASP.NET Core backend and the
    /// Railway Inventory AI service (trinova-ai-production.up.railway.app).
    ///
    /// Updated API contract:
    ///   POST /forecast — body: ForecastRequest { items: [...] }
    ///                  — response: ForecastResponse[]
    ///
    /// Before every AI call this service:
    ///   1. Queries SQL Server via ForecastDatasetRepo to build the training dataset.
    ///   2. Wraps the dataset in a ForecastRequest.
    ///   3. POSTs the payload to the AI service.
    ///   4. Deserialises the returned ForecastResponse[].
    ///
    /// Preserved behaviours:
    ///   - Exponential-backoff retry (1 s → 2 s → 4 s, max 3 retries)
    ///   - 30-second timeout per attempt
    ///   - Structured logging on every request and failure
    ///   - Graceful fallback — AI errors never propagate as HTTP 500
    ///   - Input validation before making network calls
    ///   - Health check via GET /
    /// </summary>
    public class InventoryAIService : IInventoryAIService
    {
        private readonly HttpClient                  _httpClient;
        private readonly ILogger<InventoryAIService> _logger;
        private readonly IConfiguration              _configuration;
        private readonly ForecastDatasetRepo         _datasetRepo;

        // Case-insensitive deserialiser to handle FastAPI camelCase/snake_case responses.
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Retry delays: 1 s, 2 s, 4 s — exponential backoff, max 3 retries.
        private static readonly TimeSpan[] _retryDelays =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        ];

        // Maximum allowed TopN value — prevents absurdly large responses.
        private const int MaxTopN = 200;

        public InventoryAIService(
            HttpClient                  httpClient,
            ILogger<InventoryAIService> logger,
            IConfiguration              configuration,
            ForecastDatasetRepo         datasetRepo)
        {
            _httpClient    = httpClient;
            _logger        = logger;
            _configuration = configuration;
            _datasetRepo   = datasetRepo;
        }

        // ── Public interface ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<InventoryAiApiResponse<List<InventoryForecastItem>>> GetForecastAsync(
            List<int>?        productIdFilter   = null,
            int?              topN              = null,
            CancellationToken cancellationToken = default)
        {
            // ── Input validation (reject invalid inputs before any I/O) ───────────
            if (productIdFilter is not null)
            {
                var invalid = productIdFilter.Where(id => id <= 0).ToList();
                if (invalid.Count > 0)
                {
                    _logger.LogWarning(
                        "Inventory AI: GetForecastAsync rejected — invalid product IDs: {Ids}",
                        string.Join(", ", invalid));

                    return InventoryAiApiResponse<List<InventoryForecastItem>>.Fail(
                        $"Invalid product IDs: {string.Join(", ", invalid)}. All IDs must be ≥ 1.");
                }
            }

            if (topN.HasValue && (topN.Value < 1 || topN.Value > MaxTopN))
            {
                return InventoryAiApiResponse<List<InventoryForecastItem>>.Fail(
                    $"topN must be between 1 and {MaxTopN}.");
            }

            // ── Build dataset from SQL Server ─────────────────────────────────────
            ForecastRequest payload;
            try
            {
                _logger.LogInformation(
                    "Inventory AI: querying SQL Server for forecast dataset via ForecastDatasetRepo.");

                var dataset = await _datasetRepo.GetForecastDatasetAsync();

                if (dataset.Count == 0)
                {
                    _logger.LogWarning(
                        "Inventory AI: forecast dataset is empty — no OUT transactions found in stock_transaction.");

                    return InventoryAiApiResponse<List<InventoryForecastItem>>.Fail(
                        "No stock usage data available to generate a forecast.");
                }

                payload = new ForecastRequest { Items = dataset };

                _logger.LogInformation(
                    "Inventory AI: dataset built. {Count} rows will be sent to POST /forecast.",
                    dataset.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Inventory AI: failed to retrieve forecast dataset from SQL Server.");

                return InventoryAiApiResponse<List<InventoryForecastItem>>.Fail(
                    "Failed to retrieve inventory data from the database.");
            }

            // ── POST to AI service ────────────────────────────────────────────────
            var result = await PostAsync<ForecastRequest, List<InventoryForecastItem>>(
                "/forecast", payload, cancellationToken);

            if (!result.success || result.data is null)
                return result;

            var items = result.data;

            // ── Apply optional product-ID filter ──────────────────────────────────
            if (productIdFilter is { Count: > 0 })
            {
                var filterSet = new HashSet<int>(productIdFilter);
                items = items.Where(i => filterSet.Contains(i.ProductId)).ToList();
            }

            // ── Apply optional top-N cap (highest forecast volume first) ──────────
            if (topN.HasValue)
            {
                items = items
                    .OrderByDescending(i => i.ForecastNextMonth)
                    .Take(topN.Value)
                    .ToList();
            }

            if (items.Count == 0)
            {
                return InventoryAiApiResponse<List<InventoryForecastItem>>.Fail(
                    "No forecast data found for the specified criteria.");
            }

            return InventoryAiApiResponse<List<InventoryForecastItem>>.Ok(
                items, $"Forecast retrieved successfully. {items.Count} product(s) returned.");
        }

        /// <inheritdoc/>
        public async Task<InventoryAiApiResponse<InventoryForecastItem>> GetProductForecastAsync(
            int               productId,
            CancellationToken cancellationToken = default)
        {
            // ── Validation ────────────────────────────────────────────────────────
            if (productId <= 0)
            {
                return InventoryAiApiResponse<InventoryForecastItem>.Fail(
                    "Product ID must be ≥ 1.");
            }

            // ── Fetch full list via POST /forecast, then filter ───────────────────
            var allResult = await GetForecastAsync(
                productIdFilter: null,
                topN:            null,
                cancellationToken);

            if (!allResult.success || allResult.data is null)
            {
                return InventoryAiApiResponse<InventoryForecastItem>.Fail(allResult.message);
            }

            var item = allResult.data.FirstOrDefault(i => i.ProductId == productId);

            if (item is null)
            {
                return InventoryAiApiResponse<InventoryForecastItem>.Fail(
                    $"No forecast found for product ID {productId}.");
            }

            return InventoryAiApiResponse<InventoryForecastItem>.Ok(
                item, $"Forecast for product {productId} retrieved successfully.");
        }

        /// <inheritdoc/>
        public async Task<InventoryAiHealthResponse> CheckHealthAsync(
            CancellationToken cancellationToken = default)
        {
            var url = _configuration["ExternalServices:InventoryAIBaseUrl"] ?? string.Empty;
            var sw  = Stopwatch.StartNew();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5)); // tight timeout for health checks

                var response = await _httpClient.GetAsync("/", cts.Token);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Inventory AI health check: connected. Latency: {Latency} ms",
                        sw.ElapsedMilliseconds);

                    return new InventoryAiHealthResponse
                    {
                        connected  = true,
                        service    = "Inventory AI",
                        url        = url,
                        latency_ms = sw.ElapsedMilliseconds
                    };
                }

                _logger.LogWarning(
                    "Inventory AI health check: service responded {StatusCode}",
                    (int)response.StatusCode);

                return new InventoryAiHealthResponse
                {
                    connected  = false,
                    service    = "Inventory AI",
                    url        = url,
                    latency_ms = sw.ElapsedMilliseconds,
                    error      = $"Service returned HTTP {(int)response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "Inventory AI health check: unreachable");

                return new InventoryAiHealthResponse
                {
                    connected = false,
                    service   = "Inventory AI",
                    url       = url,
                    error     = "Service is unreachable"
                };
            }
        }

        // ── Core HTTP POST helper with retry + timeout + logging ──────────────────

        /// <summary>
        /// POSTs <paramref name="body"/> as JSON to <paramref name="endpoint"/> on
        /// the AI service and deserialises the response as
        /// <typeparamref name="TResponse"/>.
        ///
        /// Retry policy — exponential backoff, transient errors only:
        ///   Attempt 1 — immediate
        ///   Retry  1  — wait 1 s  (network / 5xx)
        ///   Retry  2  — wait 2 s
        ///   Retry  3  — wait 4 s
        ///
        /// Validation errors (HTTP 4xx) are NOT retried.
        /// Timeout and connectivity failures are caught and returned as a failed
        /// InventoryAiApiResponse — never re-thrown to the caller.
        /// </summary>
        private async Task<InventoryAiApiResponse<TResponse>> PostAsync<TBody, TResponse>(
            string            endpoint,
            TBody             body,
            CancellationToken cancellationToken)
            where TBody     : class
            where TResponse : class
        {
            var sw = Stopwatch.StartNew();

            for (int attempt = 0; attempt <= _retryDelays.Length; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Inventory AI → POST {Endpoint} (attempt {Attempt})",
                        endpoint, attempt + 1);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    var response = await _httpClient.PostAsJsonAsync(endpoint, body, _jsonOpts, cts.Token);

                    sw.Stop();
                    _logger.LogInformation(
                        "Inventory AI ← POST {Endpoint} | Status: {Status} | Elapsed: {Elapsed} ms",
                        endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);

                    // 4xx from FastAPI — do not retry (bad request, validation error, etc.)
                    if ((int)response.StatusCode is >= 400 and < 500)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning(
                            "Inventory AI client error at POST {Endpoint} (HTTP {Status}): {Body}",
                            endpoint, (int)response.StatusCode, errorBody);

                        return InventoryAiApiResponse<TResponse>.Fail(
                            $"Inventory AI service returned a client error: HTTP {(int)response.StatusCode}.");
                    }

                    // 5xx — transient, retry
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning(
                            "Inventory AI server error at POST {Endpoint} (HTTP {Status}): {Body}",
                            endpoint, (int)response.StatusCode, errorBody);

                        if (attempt < _retryDelays.Length)
                        {
                            await Task.Delay(_retryDelays[attempt], cancellationToken);
                            sw.Restart();
                            continue;
                        }

                        return InventoryAiApiResponse<TResponse>.Fail(
                            "Inventory AI service returned an unexpected error. Please try again later.");
                    }

                    // ── Deserialise ───────────────────────────────────────────────
                    var result = await response.Content
                        .ReadFromJsonAsync<TResponse>(_jsonOpts, cancellationToken);

                    if (result is null)
                    {
                        return InventoryAiApiResponse<TResponse>.Fail(
                            "Inventory AI service returned an empty response.");
                    }

                    return InventoryAiApiResponse<TResponse>.Ok(result);
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Client cancelled — do not retry, do not log as error
                    sw.Stop();
                    _logger.LogInformation(
                        "Inventory AI request to POST {Endpoint} was cancelled by the client.", endpoint);

                    return InventoryAiApiResponse<TResponse>.Fail(
                        "Request was cancelled.");
                }
                catch (TaskCanceledException)
                {
                    // Our 30-second timeout fired
                    sw.Stop();
                    _logger.LogWarning(
                        "Inventory AI timeout at POST {Endpoint} after {Elapsed} ms. Attempt {Attempt}/{Max}",
                        endpoint, sw.ElapsedMilliseconds, attempt + 1, _retryDelays.Length + 1);

                    if (attempt < _retryDelays.Length)
                    {
                        await Task.Delay(_retryDelays[attempt], cancellationToken);
                        sw.Restart();
                        continue;
                    }

                    return InventoryAiApiResponse<TResponse>.Fail(
                        "Inventory AI service timed out. Please try again.");
                }
                catch (HttpRequestException ex)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        ex,
                        "Inventory AI connectivity error at POST {Endpoint}. Attempt {Attempt}/{Max}. Elapsed: {Elapsed} ms",
                        endpoint, attempt + 1, _retryDelays.Length + 1, sw.ElapsedMilliseconds);

                    if (attempt < _retryDelays.Length)
                    {
                        await Task.Delay(_retryDelays[attempt], cancellationToken);
                        sw.Restart();
                        continue;
                    }

                    return InventoryAiApiResponse<TResponse>.Fail(
                        "Inventory AI service is temporarily unavailable. Please try again later.");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    // Non-transient — do not retry, never expose stack trace
                    _logger.LogError(
                        ex,
                        "Inventory AI unexpected error at POST {Endpoint}. Elapsed: {Elapsed} ms",
                        endpoint, sw.ElapsedMilliseconds);

                    return InventoryAiApiResponse<TResponse>.Fail(
                        "An unexpected error occurred while contacting the Inventory AI service.");
                }
            }

            // Compiler satisfaction — loop above always returns or continues
            return InventoryAiApiResponse<TResponse>.Fail(
                "Inventory AI service is temporarily unavailable.");
        }
    }
}
