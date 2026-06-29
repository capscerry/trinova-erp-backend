using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    public class SupplierRiskController : ControllerBase
    {
        private readonly ISupplierRiskUsecase _supplierRiskUsecase;

        public SupplierRiskController(ISupplierRiskUsecase supplierRiskUsecase)
        {
            _supplierRiskUsecase = supplierRiskUsecase;
        }

        // ── Predict ───────────────────────────────────────────────────────────

        /// <summary>
        /// Aggregates live ERP metrics for a supplier and calls the XGBoost model
        /// to predict whether that supplier is LOW / MEDIUM / HIGH risk.
        ///
        /// ERP → FastAPI field mapping:
        ///   total_po_value    → supplier_price
        ///   avg_delivery_days → lead_time_days
        ///   claim_rate        → claim_rate
        ///   on_time_rate      → on_time_rate
        ///   total_orders      → order_frequency
        /// </summary>
        [HttpGet("/api/supplier-risk/predict/{supplierId}")]
        public async Task<IActionResult> PredictSupplierRisk(int supplierId)
        {
            try
            {
                var result = await _supplierRiskUsecase.PredictSupplierRisk(supplierId);
                return Ok(new { status = true, message = "Prediction successful", data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { status = false, message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        // ── Train — bundled server CSV ────────────────────────────────────────

        /// <summary>
        /// Retrains using the bundled CSV already on the FastAPI server's disk.
        /// POST body is optional — leave empty or pass { "csv_path": "..." } for
        /// a custom server-side path.
        /// </summary>
        [HttpPost("/api/supplier-risk/train")]
        public async Task<IActionResult> TrainFromServerCsv([FromBody] TrainFromCsvPathRequest? request)
        {
            try
            {
                var result = await _supplierRiskUsecase.TrainFromServerCsv(request?.csv_path);
                return Ok(new { status = true, message = "Model retrained successfully from server CSV", data = result });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        // ── Train — live ERP data ─────────────────────────────────────────────

        /// <summary>
        /// Queries the ERP database (PO + GR + Purchase Return), builds training rows
        /// with the FastAPI-required columns, and calls POST /train/from-rows.
        ///
        /// ERP → FastAPI column mapping:
        ///   total_po_value    → supplier_price
        ///   avg_delivery_days → lead_time_days
        ///   claim_rate        → claim_rate
        ///   on_time_rate      → on_time_rate
        ///   total_orders      → order_frequency
        ///   derived label     → late_delivery  (1 if claim_rate>0.1 or on_time_rate&lt;0.8)
        ///
        /// Set append_to_existing=true (default) to merge with the bundled dataset.
        /// </summary>
        [HttpPost("/api/supplier-risk/train/from-erp")]
        public async Task<IActionResult> TrainFromErpData([FromBody] TrainFromErpRequest? request)
        {
            try
            {
                bool append = request?.append_to_existing ?? true;
                var  result = await _supplierRiskUsecase.TrainFromErpData(append);
                return Ok(new { status = true, message = "Model retrained successfully from live ERP data", data = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        // ── Train — manual JSON rows ──────────────────────────────────────────

        /// <summary>
        /// Accepts pre-labeled training rows as JSON and forwards them directly
        /// to POST /train/from-rows. Each row must include:
        ///   supplier_price, lead_time_days, claim_rate, on_time_rate, order_frequency, late_delivery
        /// </summary>
        [HttpPost("/api/supplier-risk/train/from-rows")]
        public async Task<IActionResult> TrainFromRows([FromBody] SupplierRiskTrainFromRowsRequest request)
        {
            if (request?.rows == null || request.rows.Count == 0)
                return BadRequest(new { status = false, message = "rows must not be empty." });

            try
            {
                // These rows already have the correct FastAPI column names —
                // forward straight to the usecase which calls /train/from-rows.
                // We reuse TrainFromErpData only for the SQL-derived path;
                // for manual rows we call the endpoint directly via HttpClient.
                var client = HttpContext.RequestServices
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("XGBoost");

                var jsonOpts = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy        = System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
                    PropertyNameCaseInsensitive = true
                };

                var response = await client.PostAsJsonAsync("/train/from-rows", request, jsonOpts);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode,
                        new { status = false, message = $"XGBoost service error: {body}" });
                }

                var result = await response.Content.ReadFromJsonAsync<SupplierRiskTrainResponse>(jsonOpts);
                return Ok(new { status = true, message = "Model retrained successfully from provided rows", data = result });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        // ── Train — CSV file upload ───────────────────────────────────────────

        /// <summary>
        /// Accepts a CSV file and forwards it to POST /train/from-csv-upload.
        /// Required columns: supplier_price, lead_time_days, claim_rate,
        ///                   on_time_rate, order_frequency, late_delivery
        /// </summary>
        [HttpPost("/api/supplier-risk/train/from-csv-upload")]
        public async Task<IActionResult> TrainFromCsvUpload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { status = false, message = "A CSV file is required." });

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { status = false, message = "Only .csv files are accepted." });

            try
            {
                await using var stream = file.OpenReadStream();
                var result = await _supplierRiskUsecase.TrainFromCsvUpload(stream, file.FileName);
                return Ok(new { status = true, message = "Model retrained successfully from uploaded CSV", data = result });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { status = false, message = $"XGBoost service error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
    }

    // ── Supporting request bodies ─────────────────────────────────────────────

    public class TrainFromCsvPathRequest
    {
        /// <summary>Optional absolute path on the FastAPI server's local disk.</summary>
        public string? csv_path { get; set; }
    }

    public class TrainFromErpRequest
    {
        /// <summary>
        /// When true (default), FastAPI merges the ERP rows with its bundled
        /// historical dataset before retraining.
        /// </summary>
        public bool append_to_existing { get; set; } = true;
    }
}
