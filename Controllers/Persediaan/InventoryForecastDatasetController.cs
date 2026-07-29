using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    /// <summary>
    /// Internal-only endpoint for the Inventory AI service (trinova-ai) running
    /// on Railway.  Returns aggregated stock-usage data for demand-forecast
    /// model training.
    ///
    /// Route:
    ///   GET /api/inventory/forecast-dataset — returns JSON array of usage rows
    ///
    /// Auth:
    ///   [AllowAnonymous] — This endpoint has NO user authentication requirement.
    ///   The Railway AI service calls it without a JWT token.
    ///
    /// Data returned:
    ///   product_id, product_name, tahun, bulan, total_usage
    ///   (snake_case field names to match the pandas DataFrame the AI expects)
    ///
    /// Architecture note:
    ///   Matches the Purchasing AI architecture:
    ///     - Purchasing AI: .NET backend calls Railway FastAPI with no auth
    ///     - Inventory AI:  Railway FastAPI calls .NET backend with no auth
    ///   Both AI services (trinova-service-ml and trinova-ai) are unauthenticated
    ///   because they serve internal ML tasks, not user-facing data endpoints.
    ///
    /// The data returned (monthly aggregated usage counts per product) is not
    /// sensitive and contains no PII, pricing, or supplier information.
    /// </summary>
    [ApiController]
    [Route("api/inventory/forecast-dataset")]
    public class InventoryForecastDatasetController : ControllerBase
    {
        private readonly ForecastDatasetRepo _repo;
        private readonly ILogger<InventoryForecastDatasetController> _logger;

        public InventoryForecastDatasetController(
            ForecastDatasetRepo repo,
            ILogger<InventoryForecastDatasetController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Returns the complete stock-usage dataset for demand-forecast training.
        ///
        /// This endpoint is consumed by the Python trinova-ai service on every
        /// GET /forecast call.  It replaces the former direct SQL Server connection
        /// the Python service used via pyodbc.
        ///
        /// Response: JSON array of objects, each with:
        ///   { "product_id": int, "product_name": string,
        ///     "tahun": int, "bulan": int, "total_usage": double }
        ///
        /// Ordering: (product_id, tahun, bulan) ascending — matches the DataFrame
        /// sort applied by client/inventory_client.py.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetForecastDataset()
        {
            try
            {
                _logger.LogInformation(
                    "Inventory AI forecast-dataset: request received from {IP}",
                    HttpContext.Connection.RemoteIpAddress);

                var dataset = await _repo.GetForecastDatasetAsync();

                _logger.LogInformation(
                    "Inventory AI forecast-dataset: returning {Count} rows ({ProductCount} products)",
                    dataset.Count,
                    dataset.Select(r => r.ProductId).Distinct().Count());

                // Return the raw array — no wrapper object.
                // The Python client expects: List[ForecastDatasetItem]
                return Ok(dataset);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,
                    "Inventory AI forecast-dataset: database error (SqlErrorNumber={Number})",
                    ex.Number);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "Database error while generating forecast dataset.",
                    code = "DB_ERROR",
                    traceId = HttpContext.TraceIdentifier
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Inventory AI forecast-dataset: unexpected error");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "An unexpected error occurred while generating forecast dataset.",
                    code = "INTERNAL_ERROR",
                    traceId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}
