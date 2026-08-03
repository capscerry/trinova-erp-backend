using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Security;
using trinova_erp_backend.Services;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    // NOTE: this controller previously had no [Authorize] attribute at all,
    // so any authenticated user of ANY role (Sales, Warehouse, etc.) could
    // retrain the supplier-risk ML model. Scoped to Admin/Purchasing to match
    // SupplierProductController's convention — this is a broken-access-control
    // fix, not just a file-upload hardening change.
    [Authorize(Roles = Roles.PurchasingAccess)]
    public class SupplierRiskController : ControllerBase
    {
        private readonly ISupplierRiskUsecase _supplierRiskUsecase;
        private readonly IConfiguration _configuration;
        private readonly IActivityLogService _activityLogService;

        public SupplierRiskController(
            ISupplierRiskUsecase supplierRiskUsecase,
            IConfiguration configuration,
            IActivityLogService activityLogService)
        {
            _supplierRiskUsecase = supplierRiskUsecase;
            _configuration = configuration;
            _activityLogService = activityLogService;
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

        // ── Unified recommendation (single source of truth) ──────────────────────

        /// <summary>
        /// Single source of truth for all purchasing recommendation screens.
        ///
        /// Runs: batch ML predict → AHP-TOPSIS ranking → profile derivation.
        /// No model retraining. No independent scoring on the frontend.
        ///
        /// Response shape:
        /// {
        ///   "ranking": {
        ///     "total": ..., "consistency_ratio": ..., "ahp_weights": {...},
        ///     "ranked_suppliers": [ { topsis_rank, topsis_score, supplier_name, ... }, ... ]
        ///   },
        ///   "profiles": {
        ///     "Balanced":        { profile, supplier_id, supplier_name, topsis_score, topsis_rank, ... },
        ///     "High Urgency":    { ... },
        ///     "Budget Priority": { ... },
        ///     "Quality Focus":   { ... }
        ///   }
        /// }
        ///
        /// Every purchasing screen — Dashboard, Supplier Recommendation page, and
        /// Purchase Order modal — must consume this endpoint and display its data
        /// as-is. No re-ranking, no re-scoring, no profile recalculation in the UI.
        ///
        /// Optional query param: pass ahp_matrix as JSON body to override AHP weights.
        /// </summary>
        [HttpGet("/api/supplier-risk/recommendation")]
        public async Task<IActionResult> GetRecommendation()
        {
            try
            {
                var result = await _supplierRiskUsecase.GetRecommendation();
                return Ok(new { status = true, message = "Supplier recommendation successful", data = result });
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

        // ── Batch predict (ML results only) ──────────────────────────────────────

        /// <summary>
        /// Aggregates live ERP data for ALL active suppliers and scores each one
        /// through the XGBoost model. Returns raw ML results (risk_level +
        /// delay_probability) with no ranking applied — display these first.
        ///
        /// Next step: pass the results array to POST /api/supplier-risk/rank/ahp-topsis.
        /// </summary>
        [HttpGet("/api/supplier-risk/predict/all")]
        public async Task<IActionResult> PredictAllSuppliers()
        {
            try
            {
                var result = await _supplierRiskUsecase.PredictAllSuppliers();
                return Ok(new { status = true, message = "Batch ML prediction successful", data = result });
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

        // ── AHP-TOPSIS ranking ────────────────────────────────────────────────────

        /// <summary>
        /// Ranks ML-scored suppliers using AHP-derived weights and TOPSIS.
        /// Pass the results array from GET /api/supplier-risk/predict/all directly
        /// in the request body.
        ///
        /// The ahp_matrix field is optional — omit it to use the server-side
        /// defaults (delay_probability weighted highest).
        ///
        /// Response is sorted by topsis_rank ascending (rank 1 = best supplier).
        /// </summary>
        [HttpPost("/api/supplier-risk/rank/ahp-topsis")]
        public async Task<IActionResult> RankWithAhpTopsis([FromBody] RankRequest request)
        {
            if (request?.suppliers == null || request.suppliers.Count == 0)
                return BadRequest(new { status = false, message = "suppliers must not be empty." });

            try
            {
                var result = await _supplierRiskUsecase.RankWithAhpTopsis(
                    request.suppliers,
                    request.ahp_matrix?.matrix);
                return Ok(new { status = true, message = "AHP-TOPSIS ranking successful", data = result });
            }
            catch (ArgumentException ex)
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

        // ── Full end-to-end: train → predict → rank ───────────────────────────────

        /// <summary>
        /// One-shot endpoint that runs the complete supplier evaluation pipeline:
        ///   1. Retrain XGBoost using the bundled historical CSV merged with
        ///      live ERP data (append_erp_to_historical = true by default).
        ///   2. Batch-predict all active ERP suppliers with the fresh model.
        ///   3. Rank the ML results with AHP-TOPSIS.
        ///
        /// The response contains three sections:
        ///   - train_result   : XGBoost training metrics
        ///   - ml_results     : raw per-supplier ML scores (show these first in UI)
        ///   - ranked_results : AHP-TOPSIS ranked list
        ///
        /// POST body is optional. Example to pass a custom AHP matrix:
        /// {
        ///   "append_erp_to_historical": true,
        ///   "ahp_matrix": [[1,3,5,5,7],[0.33,1,3,3,5],[0.2,0.33,1,1,3],[0.2,0.33,1,1,3],[0.14,0.2,0.33,0.33,1]]
        /// }
        /// </summary>
        [HttpPost("/api/supplier-risk/evaluate/all")]
        public async Task<IActionResult> TrainAndEvaluateAll(
            [FromBody] TrainAndEvaluateRequest? request)
        {
            try
            {
                bool append   = request?.append_erp_to_historical ?? true;
                var  matrix   = request?.ahp_matrix;
                var  result   = await _supplierRiskUsecase.TrainAndEvaluateAll(append, matrix);
                return Ok(new { status = true, message = "Full supplier risk evaluation complete", data = result });
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
            var maxBytes = _configuration.GetValue<long>(
                "FileUploadSecurity:MaxCsvSizeBytes", 10 * 1024 * 1024);

            var validation = FileUploadSecurity.ValidateCsv(file, maxBytes);
            if (!validation.IsValid)
            {
                await _activityLogService.LogAsync(new ActivityLogCreate
                {
                    Module = "security",
                    ActivityType = "file_upload_rejected",
                    Title = $"Supplier-risk CSV upload rejected: {validation.ErrorMessage}",
                    Description = $"FileName={file?.FileName}, Size={file?.Length}, ContentType={file?.ContentType}",
                    RefTable = "supplier_risk_model"
                });

                return StatusCode(validation.StatusCode, new { status = false, message = validation.ErrorMessage });
            }

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

    /// <summary>
    /// Optional request body for POST /api/supplier-risk/evaluate/all.
    /// All fields are optional — omit the body entirely to run with defaults.
    /// </summary>
    public class TrainAndEvaluateRequest
    {
        /// <summary>
        /// When true (default), live ERP rows are merged with the bundled
        /// historical CSV before retraining, preserving historical signal.
        /// </summary>
        public bool append_erp_to_historical { get; set; } = true;

        /// <summary>
        /// Optional custom 5×5 AHP pairwise comparison matrix (row-major).
        /// Row/col order: delay_probability, on_time_rate, claim_rate,
        ///                supplier_price, order_frequency.
        /// Omit to use the server-side defaults.
        /// </summary>
        public List<List<double>>? ahp_matrix { get; set; }
    }
}
