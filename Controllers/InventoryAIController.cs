using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using trinova_erp_backend.Models.AI;
using trinova_erp_backend.Services.InventoryAI;

namespace trinova_erp_backend.Controllers
{
    /// <summary>
    /// Exposes Inventory AI endpoints to the frontend.
    ///
    /// The frontend must ONLY call these endpoints — never the Railway AI service directly.
    ///
    /// Routes:
    ///   POST  /api/inventory-ai/recommend  — forecast-based restock recommendations
    ///   POST  /api/inventory-ai/predict    — forecast for a single product
    ///   GET   /api/inventory-ai/health     — AI service connectivity check
    ///
    /// This controller is intentionally thin.
    /// All business logic, HTTP calls, retry, and logging live in IInventoryAIService.
    /// </summary>
    [ApiController]
    [Route("api/inventory-ai")]
    [Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class InventoryAIController : ControllerBase
    {
        private readonly IInventoryAIService         _inventoryAI;
        private readonly IMemoryCache                _cache;
        private readonly ILogger<InventoryAIController> _logger;

        // Cache key prefix and TTL — avoids duplicate AI calls within the same session window.
        private const string ForecastCacheKeyPrefix = "inventory_ai_forecast_";
        private static readonly TimeSpan CacheTtl   = TimeSpan.FromMinutes(5);

        public InventoryAIController(
            IInventoryAIService            inventoryAI,
            IMemoryCache                   cache,
            ILogger<InventoryAIController> logger)
        {
            _inventoryAI = inventoryAI;
            _cache       = cache;
            _logger      = logger;
        }

        // ── POST /api/inventory-ai/recommend ─────────────────────────────────────

        /// <summary>
        /// Returns AI-driven inventory restock recommendations based on forecast data.
        ///
        /// Validation rules (HTTP 400 returned without calling AI):
        ///   - ProductIds, when provided, must contain only positive integers.
        ///   - TopN, when provided, must be between 1 and 200.
        ///
        /// Performance: responses are cached per unique request signature for 5 minutes
        /// to avoid duplicate AI calls within the same user session.
        /// </summary>
        [HttpPost("recommend")]
        public async Task<IActionResult> Recommend(
            [FromBody] InventoryForecastRecommendRequest request,
            CancellationToken cancellationToken)
        {
            // ── Input validation ──────────────────────────────────────────────────
            if (request.ProductIds is not null)
            {
                if (request.ProductIds.Count == 0)
                    return BadRequest(InventoryAiApiResponse<object>.Fail(
                        "ProductIds must not be an empty list. Omit the field to fetch all products."));

                var badIds = request.ProductIds.Where(id => id <= 0).ToList();
                if (badIds.Count > 0)
                    return BadRequest(InventoryAiApiResponse<object>.Fail(
                        $"Invalid product IDs: {string.Join(", ", badIds)}. All IDs must be ≥ 1."));
            }

            if (request.TopN.HasValue && (request.TopN.Value < 1 || request.TopN.Value > 200))
                return BadRequest(InventoryAiApiResponse<object>.Fail(
                    "TopN must be between 1 and 200."));

            // ── Cache key ─────────────────────────────────────────────────────────
            var sortedIds = request.ProductIds?.OrderBy(id => id).ToList();
            var cacheKey  = $"{ForecastCacheKeyPrefix}{string.Join(",", sortedIds ?? new List<int>())}_{request.TopN}";

            if (_cache.TryGetValue(cacheKey, out InventoryAiApiResponse<List<InventoryForecastItem>>? cached)
                && cached is not null)
            {
                _logger.LogInformation(
                    "Inventory AI recommend: serving cached response for key {Key}", cacheKey);
                return Ok(cached);
            }

            // ── Delegate to service ───────────────────────────────────────────────
            var result = await _inventoryAI.GetForecastAsync(
                request.ProductIds, request.TopN, cancellationToken);

            if (!result.success)
                return Ok(result); // friendly message, not HTTP 500

            // ── Cache successful response ─────────────────────────────────────────
            _cache.Set(cacheKey, result, CacheTtl);

            return Ok(result);
        }

        // ── POST /api/inventory-ai/predict ───────────────────────────────────────

        /// <summary>
        /// Returns the demand forecast for a single product by ID.
        ///
        /// Validation: ProductId must be ≥ 1.
        /// </summary>
        [HttpPost("predict")]
        public async Task<IActionResult> Predict(
            [FromBody] InventoryProductPredictRequest request,
            CancellationToken cancellationToken)
        {
            // ── Input validation ──────────────────────────────────────────────────
            if (request.ProductId <= 0)
                return BadRequest(InventoryAiApiResponse<object>.Fail(
                    "ProductId must be ≥ 1."));

            // ── Check per-product cache ───────────────────────────────────────────
            var cacheKey = $"{ForecastCacheKeyPrefix}product_{request.ProductId}";

            if (_cache.TryGetValue(cacheKey, out InventoryAiApiResponse<InventoryForecastItem>? cached)
                && cached is not null)
            {
                _logger.LogInformation(
                    "Inventory AI predict: serving cached response for product {ProductId}",
                    request.ProductId);
                return Ok(cached);
            }

            // ── Delegate to service ───────────────────────────────────────────────
            var result = await _inventoryAI.GetProductForecastAsync(
                request.ProductId, cancellationToken);

            if (!result.success)
                return Ok(result); // friendly failure, never HTTP 500

            _cache.Set(cacheKey, result, CacheTtl);

            return Ok(result);
        }

        // ── GET /api/inventory-ai/health ─────────────────────────────────────────

        /// <summary>
        /// Probes the Inventory AI service and returns connectivity + latency.
        ///
        /// This endpoint intentionally has no auth restriction so monitoring
        /// tools can poll it. The response never exposes secrets or internal URLs.
        ///
        /// Response shape:
        ///   { "connected": true,  "service": "Inventory AI", "latency_ms": 123 }
        ///   { "connected": false, "service": "Inventory AI", "error": "..." }
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            var result = await _inventoryAI.CheckHealthAsync(cancellationToken);

            // Return 200 even when disconnected — the payload carries the status.
            // Callers should check result.connected, not the HTTP status code.
            return Ok(result);
        }
    }
}
