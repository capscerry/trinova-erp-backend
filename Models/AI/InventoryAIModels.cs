using System.Text.Json.Serialization;

namespace trinova_erp_backend.Models.AI
{
    // ─── Forecast response — matches GET /forecast ForecastResponse schema exactly ─

    /// <summary>
    /// Strongly typed response from GET /forecast.
    /// Field names match the FastAPI ForecastResponse schema discovered via /openapi.json.
    /// </summary>
    public class InventoryForecastItem
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>e.g. "2026-08"</summary>
        [JsonPropertyName("forecast_month")]
        public string ForecastMonth { get; set; } = string.Empty;

        /// <summary>e.g. "2026-01 to 2026-07"</summary>
        [JsonPropertyName("last_training_period")]
        public string LastTrainingPeriod { get; set; } = string.Empty;

        /// <summary>Number of historical records used to train the model.</summary>
        [JsonPropertyName("historical_records")]
        public int HistoricalRecords { get; set; }

        /// <summary>Forecasted quantity demand for the next month.</summary>
        [JsonPropertyName("forecast_next_month")]
        public double ForecastNextMonth { get; set; }

        /// <summary>ISO-8601 timestamp when the forecast was generated.</summary>
        [JsonPropertyName("generated_at")]
        public string GeneratedAt { get; set; } = string.Empty;
    }

    // ─── Envelope returned by every InventoryAIController endpoint ───────────────

    /// <summary>
    /// Unified response wrapper for all /api/inventory-ai endpoints.
    /// Mirrors the pattern used by AiApiResponse&lt;T&gt; in PurchasingAI.
    /// </summary>
    public class InventoryAiApiResponse<T>
    {
        public bool   success { get; set; }
        public string message { get; set; } = string.Empty;
        public T?     data    { get; set; }

        public static InventoryAiApiResponse<T> Ok(T data, string message = "Success") =>
            new() { success = true,  message = message, data = data };

        public static InventoryAiApiResponse<T> Fail(string message) =>
            new() { success = false, message = message, data = default };
    }

    // ─── Forecast recommendation request (frontend → backend) ────────────────────

    /// <summary>
    /// Frontend-facing POST body for POST /api/inventory-ai/recommend.
    /// Optional filters allow the caller to narrow results.
    /// The backend always fetches fresh data from the AI service.
    /// </summary>
    public class InventoryForecastRecommendRequest
    {
        /// <summary>
        /// Optional list of product IDs to filter recommendations.
        /// When null or empty, all forecasted products are returned.
        /// Must contain only positive integers when provided.
        /// </summary>
        public List<int>? ProductIds { get; set; }

        /// <summary>
        /// Optional maximum number of results to return (top-N by forecast volume).
        /// Must be between 1 and 200 when provided.
        /// </summary>
        public int? TopN { get; set; }
    }

    // ─── Predict request (frontend → backend) ────────────────────────────────────

    /// <summary>
    /// Frontend-facing POST body for POST /api/inventory-ai/predict.
    /// Targets a single product by ID for a focused forecast lookup.
    /// </summary>
    public class InventoryProductPredictRequest
    {
        /// <summary>ERP product ID. Must be ≥ 1.</summary>
        public int ProductId { get; set; }
    }

    // ─── Health check response ────────────────────────────────────────────────────

    /// <summary>
    /// Response from GET /api/inventory-ai/health.
    /// Exposes AI service connectivity status and round-trip latency.
    /// </summary>
    public class InventoryAiHealthResponse
    {
        public bool    connected  { get; set; }
        public string  service    { get; set; } = "Inventory AI";
        public string  url        { get; set; } = string.Empty;

        /// <summary>Round-trip latency in milliseconds. Null when unreachable.</summary>
        public long?   latency_ms { get; set; }

        /// <summary>Human-readable failure reason when connected is false.</summary>
        public string? error      { get; set; }
    }
}
