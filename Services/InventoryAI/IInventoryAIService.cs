using trinova_erp_backend.Models.AI;

namespace trinova_erp_backend.Services.InventoryAI
{
    /// <summary>
    /// Abstracts all communication between the ASP.NET Core backend and the Railway
    /// Inventory AI service (trinova-ai-production.up.railway.app).
    ///
    /// Controllers depend on this interface — never on HttpClient directly.
    ///
    /// All methods degrade gracefully:
    ///   - AI unavailability never causes HTTP 500.
    ///   - Failures are logged and surfaced through InventoryAiApiResponse.
    ///   - Existing Inventory business logic (stock, transactions, warehouses) is
    ///     never affected by AI failures.
    ///
    /// API contract (new):
    ///   POST /forecast  — body: ForecastRequest { items: [...] }
    ///                   — response: ForecastResponse[]
    ///
    /// The service fetches the training dataset from SQL Server via
    /// ForecastDatasetRepo before every AI call so the model always
    /// receives fresh data.
    /// </summary>
    public interface IInventoryAIService
    {
        /// <summary>
        /// Fetches the forecast dataset from SQL Server, POSTs it to
        /// POST /forecast on the Railway AI service, and returns the
        /// forecast list.
        ///
        /// Applies optional product-ID filter and top-N cap after receiving
        /// the AI response.
        ///
        /// On failure returns a failed InventoryAiApiResponse — never throws.
        /// </summary>
        Task<InventoryAiApiResponse<List<InventoryForecastItem>>> GetForecastAsync(
            List<int>?        productIdFilter   = null,
            int?              topN              = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the forecast for a single product by ID.
        /// Fetches the full forecast list internally (via POST /forecast) and
        /// filters to the requested product — the AI service has no per-product
        /// endpoint.
        ///
        /// Returns a failed response when the product is not found.
        /// Never throws.
        /// </summary>
        Task<InventoryAiApiResponse<InventoryForecastItem>> GetProductForecastAsync(
            int               productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Probes the AI service root endpoint and measures round-trip latency.
        /// Never throws — always returns a response with connected = true/false.
        /// </summary>
        Task<InventoryAiHealthResponse> CheckHealthAsync(
            CancellationToken cancellationToken = default);
    }
}
