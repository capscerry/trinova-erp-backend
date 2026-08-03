using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.AI;

namespace trinova_erp_backend.Services.PurchasingAI
{
    /// <summary>
    /// Handles all communication between the ASP.NET Core backend and the Railway
    /// Purchasing AI service (trinova-ai-purchasing-production.up.railway.app).
    ///
    /// Responsibilities:
    ///   - ERP data aggregation (SQL → AI feature vectors)
    ///   - HTTP communication via IHttpClientFactory (never new HttpClient())
    ///   - JSON serialization with snake_case to match FastAPI field names
    ///   - Exponential-backoff retry for transient failures (1 s → 2 s → 4 s)
    ///   - 30-second timeout on every AI call
    ///   - Structured logging on every request and failure
    ///   - Graceful fallback — AI errors never propagate as HTTP 500
    /// </summary>
    public class PurchasingAIService : IPurchasingAIService
    {
        private readonly HttpClient           _httpClient;
        private readonly ILogger<PurchasingAIService> _logger;
        private readonly DatabaseConnection   _dbConfig;
        private readonly IConfiguration       _configuration;

        // Snake_case serialiser to match FastAPI field names exactly.
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        // Retry delays: 1 s, 2 s, 4 s (exponential backoff, max 3 retries).
        private static readonly TimeSpan[] _retryDelays =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        ];

        public PurchasingAIService(
            HttpClient                   httpClient,
            ILogger<PurchasingAIService> logger,
            IOptions<DatabaseConnection> dbConfig,
            IConfiguration               configuration)
        {
            _httpClient    = httpClient;
            _logger        = logger;
            _dbConfig      = dbConfig.Value;
            _configuration = configuration;
        }

        // ── Public interface ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<AiApiResponse<AiRecommendationResult>> GetRecommendationAsync(
            List<List<double>>? ahpMatrix = null,
            CancellationToken cancellationToken = default)
        {
            // Step 1 — aggregate ERP supplier data
            List<ErpSupplierAggregate> erpRows;
            try
            {
                erpRows = await AggregateAllSuppliersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Purchasing AI: failed to aggregate ERP supplier data");
                return AiApiResponse<AiRecommendationResult>.Fail(
                    "Failed to read supplier data from the ERP database.");
            }

            if (erpRows.Count == 0)
                return AiApiResponse<AiRecommendationResult>.Fail(
                    "No active suppliers with purchase history found.");

            // Step 2 — batch ML prediction
            var batchRequest = new AiBatchPredictRequest
            {
                suppliers = erpRows.Select(MapToBatchInput).ToList()
            };

            var batchResult = await PostAsync<AiBatchPredictRequest, AiBatchPredictResponse>(
                "/predict/all-suppliers", batchRequest, cancellationToken);

            if (!batchResult.success || batchResult.data is null)
                return AiApiResponse<AiRecommendationResult>.Fail(
                    batchResult.message);

            // Step 3 — AHP-TOPSIS ranking
            var rankRequest = new AiRankRequest
            {
                suppliers  = batchResult.data.results.Select(MapToRankInput).ToList(),
                ahp_matrix = ahpMatrix is not null ? new AiAhpMatrixRequest { matrix = ahpMatrix } : null
            };

            var rankResult = await PostAsync<AiRankRequest, AiRankResponse>(
                "/rank/ahp-topsis", rankRequest, cancellationToken);

            if (!rankResult.success || rankResult.data is null)
                return AiApiResponse<AiRecommendationResult>.Fail(rankResult.message);

            // Step 4 — derive purchasing profiles (all from the ranked list, no new math)
            var recommendation = BuildRecommendationResult(rankResult.data);

            return AiApiResponse<AiRecommendationResult>.Ok(
                recommendation, "Supplier recommendation successful");
        }

        /// <inheritdoc/>
        public async Task<AiApiResponse<AiSupplierPredictResponse>> PredictSupplierRiskAsync(
            int supplierId,
            CancellationToken cancellationToken = default)
        {
            ErpSupplierAggregate? agg;
            try
            {
                agg = await AggregateSupplierAsync(supplierId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Purchasing AI: failed to aggregate ERP data for supplier {SupplierId}", supplierId);
                return AiApiResponse<AiSupplierPredictResponse>.Fail(
                    "Failed to read supplier data from the ERP database.");
            }

            if (agg is null)
                return AiApiResponse<AiSupplierPredictResponse>.Fail(
                    $"Supplier {supplierId} not found or has no purchase history.");

            var request = new AiSupplierPredictRequest
            {
                supplier_id     = agg.supplier_id,
                supplier_price  = agg.total_po_value,
                lead_time_days  = Math.Max(1, (int)Math.Round(agg.avg_delivery_days)),
                claim_rate      = ClampRate(agg.claim_rate),
                on_time_rate    = ClampRate(agg.on_time_rate),
                order_frequency = Math.Max(1, agg.total_orders)
            };

            return await PostAsync<AiSupplierPredictRequest, AiSupplierPredictResponse>(
                "/predict/supplier-risk", request, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AiApiResponse<AiBatchPredictResponse>> BatchPredictAsync(
            CancellationToken cancellationToken = default)
        {
            List<ErpSupplierAggregate> rows;
            try
            {
                rows = await AggregateAllSuppliersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Purchasing AI: failed to aggregate ERP supplier data for batch predict");
                return AiApiResponse<AiBatchPredictResponse>.Fail(
                    "Failed to read supplier data from the ERP database.");
            }

            if (rows.Count == 0)
                return AiApiResponse<AiBatchPredictResponse>.Fail(
                    "No active suppliers with purchase history found.");

            var request = new AiBatchPredictRequest
            {
                suppliers = rows.Select(MapToBatchInput).ToList()
            };

            return await PostAsync<AiBatchPredictRequest, AiBatchPredictResponse>(
                "/predict/all-suppliers", request, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AiApiResponse<AiRankResponse>> RankSuppliersAsync(
            AiRankRequest request,
            CancellationToken cancellationToken = default)
        {
            return await PostAsync<AiRankRequest, AiRankResponse>(
                "/rank/ahp-topsis", request, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<AiHealthResponse> CheckHealthAsync(
            CancellationToken cancellationToken = default)
        {
            var url = _configuration["ExternalServices:PurchasingAIBaseUrl"] ?? string.Empty;
            var sw  = Stopwatch.StartNew();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5)); // health check has a short timeout

                var response = await _httpClient.GetAsync("/health", cts.Token);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Purchasing AI health check: connected. Latency: {Latency} ms", sw.ElapsedMilliseconds);

                    return new AiHealthResponse
                    {
                        connected  = true,
                        service    = "Purchasing AI",
                        url        = url,
                        latency_ms = sw.ElapsedMilliseconds
                    };
                }

                _logger.LogWarning(
                    "Purchasing AI health check: service responded {StatusCode}", (int)response.StatusCode);

                return new AiHealthResponse
                {
                    connected  = false,
                    service    = "Purchasing AI",
                    url        = url,
                    latency_ms = sw.ElapsedMilliseconds,
                    error      = $"Service returned HTTP {(int)response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "Purchasing AI health check: unreachable");

                return new AiHealthResponse
                {
                    connected = false,
                    service   = "Purchasing AI",
                    url       = url,
                    error     = "Service is unreachable"
                };
            }
        }

        // ── Core HTTP helper with retry + timeout + logging ───────────────────

        /// <summary>
        /// Posts <typeparamref name="TRequest"/> to the AI service and deserialises
        /// the response as <typeparamref name="TResponse"/>.
        ///
        /// Retry policy — exponential backoff, transient errors only:
        ///   Attempt 1 — immediate
        ///   Retry  1  — wait 1 s
        ///   Retry  2  — wait 2 s
        ///   Retry  3  — wait 4 s
        ///
        /// Validation errors (HTTP 4xx) are NOT retried.
        /// Timeout and connectivity failures are caught and returned as a failed
        /// AiApiResponse — never re-thrown to the controller.
        /// </summary>
        private async Task<AiApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest payload,
            CancellationToken cancellationToken)
            where TResponse : class
        {
            var sw = Stopwatch.StartNew();

            for (int attempt = 0; attempt <= _retryDelays.Length; attempt++)
            {
                try
                {
                    _logger.LogInformation(
                        "Purchasing AI → calling {Endpoint} (attempt {Attempt})",
                        endpoint, attempt + 1);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    var response = await _httpClient.PostAsJsonAsync(endpoint, payload, _jsonOpts, cts.Token);

                    sw.Stop();
                    _logger.LogInformation(
                        "Purchasing AI ← {Endpoint} | Status: {Status} | Elapsed: {Elapsed} ms",
                        endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);

                    // Validation error from FastAPI (HTTP 422 / 400) — do not retry
                    if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity ||
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning(
                            "Purchasing AI validation error at {Endpoint}: {Body}", endpoint, errorBody);
                        return AiApiResponse<TResponse>.Fail(
                            $"AI service rejected the request: {response.StatusCode}");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning(
                            "Purchasing AI non-success at {Endpoint} (HTTP {Status}): {Body}",
                            endpoint, (int)response.StatusCode, errorBody);

                        // Retry on 5xx server errors
                        if ((int)response.StatusCode >= 500 && attempt < _retryDelays.Length)
                        {
                            await Task.Delay(_retryDelays[attempt], cancellationToken);
                            sw.Restart();
                            continue;
                        }

                        return AiApiResponse<TResponse>.Fail(
                            "Supplier recommendation service returned an unexpected error.");
                    }

                    var result = await response.Content
                        .ReadFromJsonAsync<TResponse>(_jsonOpts, cancellationToken);

                    if (result is null)
                        return AiApiResponse<TResponse>.Fail(
                            "AI service returned an empty response.");

                    return AiApiResponse<TResponse>.Ok(result);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        "Purchasing AI timeout at {Endpoint} after {Elapsed} ms. Attempt {Attempt}/{Max}",
                        endpoint, sw.ElapsedMilliseconds, attempt + 1, _retryDelays.Length + 1);

                    // Timeout is transient — retry unless we've exhausted attempts
                    if (attempt < _retryDelays.Length)
                    {
                        await Task.Delay(_retryDelays[attempt], cancellationToken);
                        sw.Restart();
                        continue;
                    }

                    return AiApiResponse<TResponse>.Fail(
                        "Supplier recommendation service timed out. Please try again.");
                }
                catch (HttpRequestException ex)
                {
                    sw.Stop();
                    _logger.LogWarning(
                        ex,
                        "Purchasing AI connectivity error at {Endpoint}. Attempt {Attempt}/{Max}. Elapsed: {Elapsed} ms",
                        endpoint, attempt + 1, _retryDelays.Length + 1, sw.ElapsedMilliseconds);

                    if (attempt < _retryDelays.Length)
                    {
                        await Task.Delay(_retryDelays[attempt], cancellationToken);
                        sw.Restart();
                        continue;
                    }

                    return AiApiResponse<TResponse>.Fail(
                        "Supplier recommendation service is temporarily unavailable.");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    // Non-transient — do not retry, do not expose stack trace
                    _logger.LogError(
                        ex,
                        "Purchasing AI unexpected error at {Endpoint}. Elapsed: {Elapsed} ms",
                        endpoint, sw.ElapsedMilliseconds);

                    return AiApiResponse<TResponse>.Fail(
                        "An unexpected error occurred while contacting the AI service.");
                }
            }

            // Should never be reached, but satisfies the compiler
            return AiApiResponse<TResponse>.Fail(
                "Supplier recommendation service is temporarily unavailable.");
        }

        // ── Profile derivation ────────────────────────────────────────────────

        /// <summary>
        /// Derives the four purchasing profiles from an already-ranked list.
        /// No new math — every profile reads directly from topsis_rank-ordered results.
        ///
        ///   Balanced       — rank 1 overall (highest composite TOPSIS score)
        ///   High Urgency   — lowest lead_time_days among top-3 ranked
        ///   Budget Priority — lowest supplier_price among top-3 ranked
        ///   Quality Focus  — highest on_time_rate among top-3 ranked
        /// </summary>
        private static AiRecommendationResult BuildRecommendationResult(AiRankResponse ranked)
        {
            var allRanked = ranked.ranked_suppliers
                .OrderBy(s => s.topsis_rank)
                .ToList();

            int top     = Math.Min(3, Math.Max(allRanked.Count, 1));
            var topPool = allRanked.Take(top).ToList();
            var profiles = new Dictionary<string, AiSupplierRecommendationProfile>();

            var balanced = allRanked.FirstOrDefault();
            if (balanced is not null)
                profiles["Balanced"] = ToProfile("Balanced", balanced);

            var urgency = topPool.OrderBy(s => s.lead_time_days).ThenBy(s => s.topsis_rank).FirstOrDefault();
            if (urgency is not null)
                profiles["High Urgency"] = ToProfile("High Urgency", urgency);

            var budget = topPool.OrderBy(s => s.supplier_price).ThenBy(s => s.topsis_rank).FirstOrDefault();
            if (budget is not null)
                profiles["Budget Priority"] = ToProfile("Budget Priority", budget);

            var quality = topPool.OrderByDescending(s => s.on_time_rate).ThenBy(s => s.topsis_rank).FirstOrDefault();
            if (quality is not null)
                profiles["Quality Focus"] = ToProfile("Quality Focus", quality);

            return new AiRecommendationResult { ranking = ranked, profiles = profiles };
        }

        private static AiSupplierRecommendationProfile ToProfile(
            string profileName, AiRankedSupplierResult s) => new()
        {
            profile         = profileName,
            supplier_id     = s.supplier_id,
            supplier_name   = s.supplier_name,
            topsis_score    = s.topsis_score,
            topsis_rank     = s.topsis_rank,
            on_time_rate    = s.on_time_rate,
            claim_rate      = s.claim_rate,
            lead_time_days  = s.lead_time_days,
            supplier_price  = s.supplier_price,
            order_frequency = s.order_frequency,
            risk_level      = s.risk_level
        };

        // ── ERP data aggregation (SQL → AI feature vectors) ───────────────────

        private async Task<List<ErpSupplierAggregate>> AggregateAllSuppliersAsync()
        {
            const string sql = @"
                SELECT
                    s.supplier_id,
                    s.supplier_name,
                    ISNULL(SUM(po.total_amount), 0)                                  AS total_po_value,
                    COUNT(DISTINCT po.purchase_order_id)                             AS total_orders,
                    CASE
                        WHEN COUNT(DISTINCT po.purchase_order_id) = 0 THEN 0.0
                        ELSE CAST(COUNT(DISTINCT CASE
                                WHEN pr.purchase_return_id IS NOT NULL THEN po.purchase_order_id
                             END) AS FLOAT)
                             / COUNT(DISTINCT po.purchase_order_id)
                    END                                                              AS claim_rate,
                    CASE
                        WHEN COUNT(DISTINCT gr.goods_receipt_id) = 0 THEN 1.0
                        ELSE CAST(COUNT(DISTINCT CASE
                                WHEN gr.receipt_date <= po.expected_date THEN gr.goods_receipt_id
                             END) AS FLOAT)
                             / COUNT(DISTINCT gr.goods_receipt_id)
                    END                                                              AS on_time_rate,
                    ISNULL(AVG(CAST(DATEDIFF(day, po.order_date, gr.receipt_date) AS FLOAT)), 3)
                                                                                     AS avg_delivery_days
                FROM master_supplier         s
                LEFT JOIN purchase_order     po ON po.supplier_id       = s.supplier_id
                LEFT JOIN goods_receipt      gr ON gr.purchase_order_id = po.purchase_order_id
                LEFT JOIN purchase_return    pr ON pr.goods_receipt_id  = gr.goods_receipt_id
                WHERE s.status = 'Active'
                GROUP BY s.supplier_id, s.supplier_name
                HAVING COUNT(DISTINCT po.purchase_order_id) > 0";

            await using var conn = new SqlConnection(_dbConfig.SQLServer);
            var result = await conn.QueryAsync<ErpSupplierAggregate>(sql);
            return result.ToList();
        }

        private async Task<ErpSupplierAggregate?> AggregateSupplierAsync(int supplierId)
        {
            const string sql = @"
                SELECT
                    s.supplier_id,
                    s.supplier_name,
                    ISNULL(SUM(po.total_amount), 0)                                  AS total_po_value,
                    COUNT(DISTINCT po.purchase_order_id)                             AS total_orders,
                    CASE
                        WHEN COUNT(DISTINCT po.purchase_order_id) = 0 THEN 0.0
                        ELSE CAST(COUNT(DISTINCT CASE
                                WHEN pr.purchase_return_id IS NOT NULL THEN po.purchase_order_id
                             END) AS FLOAT)
                             / COUNT(DISTINCT po.purchase_order_id)
                    END                                                              AS claim_rate,
                    CASE
                        WHEN COUNT(DISTINCT gr.goods_receipt_id) = 0 THEN 1.0
                        ELSE CAST(COUNT(DISTINCT CASE
                                WHEN gr.receipt_date <= po.expected_date THEN gr.goods_receipt_id
                             END) AS FLOAT)
                             / COUNT(DISTINCT gr.goods_receipt_id)
                    END                                                              AS on_time_rate,
                    ISNULL(AVG(CAST(DATEDIFF(day, po.order_date, gr.receipt_date) AS FLOAT)), 3)
                                                                                     AS avg_delivery_days
                FROM master_supplier         s
                LEFT JOIN purchase_order     po ON po.supplier_id       = s.supplier_id
                LEFT JOIN goods_receipt      gr ON gr.purchase_order_id = po.purchase_order_id
                LEFT JOIN purchase_return    pr ON pr.goods_receipt_id  = gr.goods_receipt_id
                WHERE s.supplier_id = @SupplierId
                GROUP BY s.supplier_id, s.supplier_name";

            await using var conn = new SqlConnection(_dbConfig.SQLServer);
            return await conn.QuerySingleOrDefaultAsync<ErpSupplierAggregate>(
                sql, new { SupplierId = supplierId });
        }

        // ── Mapping helpers ───────────────────────────────────────────────────

        private static AiBatchSupplierInput MapToBatchInput(ErpSupplierAggregate r) => new()
        {
            supplier_id     = r.supplier_id,
            supplier_name   = r.supplier_name,
            supplier_price  = r.total_po_value,
            lead_time_days  = Math.Max(1, (int)Math.Round(r.avg_delivery_days)),
            claim_rate      = ClampRate(r.claim_rate),
            on_time_rate    = ClampRate(r.on_time_rate),
            order_frequency = Math.Max(1, r.total_orders)
        };

        private static AiRankSupplierInput MapToRankInput(AiSupplierPredictResult r) => new()
        {
            supplier_id      = r.supplier_id,
            supplier_name    = r.supplier_name,
            supplier_price   = r.supplier_price,
            lead_time_days   = r.lead_time_days,
            claim_rate       = r.claim_rate,
            on_time_rate     = r.on_time_rate,
            order_frequency  = r.order_frequency,
            risk_level       = r.risk_level,
            delay_probability = r.delay_probability,
            late_probability  = r.late_probability
        };

        private static double ClampRate(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return 0.0;
            return Math.Clamp(value, 0.0, 1.0);
        }

        // ── Internal ERP aggregate model ──────────────────────────────────────

        /// <summary>Result of the internal SQL aggregation. Never exposed outside this class.</summary>
        private class ErpSupplierAggregate
        {
            public int    supplier_id       { get; set; }
            public string supplier_name     { get; set; } = string.Empty;
            public double total_po_value    { get; set; }
            public double avg_delivery_days { get; set; }
            public double claim_rate        { get; set; }
            public double on_time_rate      { get; set; }
            public int    total_orders      { get; set; }
        }
    }
}
