namespace trinova_erp_backend.Models.DTO
{
    // ─── Predict ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sent to POST /predict/supplier-risk on the FastAPI service.
    /// Field names match the FastAPI SupplierInput Pydantic model exactly.
    /// </summary>
    public class SupplierRiskPredictRequest
    {
        /// <summary>Optional — echoed back in the response for traceability.</summary>
        public int? supplier_id      { get; set; }

        /// <summary>Unit price or contract value in local currency (> 0).</summary>
        public double supplier_price  { get; set; }

        /// <summary>Agreed lead time in calendar days (>= 1).</summary>
        public int    lead_time_days  { get; set; }

        /// <summary>Historical claim / defect rate (0.0 – 1.0).</summary>
        public double claim_rate      { get; set; }

        /// <summary>Historical on-time delivery rate (0.0 – 1.0).</summary>
        public double on_time_rate    { get; set; }

        /// <summary>Number of orders placed with this supplier per year.</summary>
        public int    order_frequency { get; set; }
    }

    /// <summary>
    /// Response from POST /predict/supplier-risk.
    /// Matches FastAPI's SupplierRiskResponse schema.
    /// </summary>
    public class SupplierRiskPredictResponse
    {
        /// <summary>Echoed from the request when supplied.</summary>
        public int?   supplier_id       { get; set; }

        /// <summary>"LOW", "MEDIUM", or "HIGH".</summary>
        public string risk_level        { get; set; } = string.Empty;

        /// <summary>Probability of a late delivery (0.0 – 1.0).</summary>
        public double delay_probability { get; set; }

        /// <summary>delay_probability expressed as a percentage (0 – 100).</summary>
        public int    late_probability  { get; set; }
    }

    // ─── Train — JSON rows ───────────────────────────────────────────────────────

    /// <summary>
    /// One training row. Field names must match FastAPI's REQUIRED_COLUMNS:
    /// supplier_price, lead_time_days, claim_rate, on_time_rate, order_frequency, late_delivery
    /// </summary>
    public class SupplierRiskTrainingRow
    {
        /// <summary>Optional — included for traceability but not used as a feature.</summary>
        public int?   supplier_id      { get; set; }

        public double supplier_price   { get; set; }
        public int    lead_time_days   { get; set; }
        public double claim_rate       { get; set; }
        public double on_time_rate     { get; set; }
        public int    order_frequency  { get; set; }

        /// <summary>Ground-truth label: 0 = on-time, 1 = late.</summary>
        public int    late_delivery    { get; set; }
    }

    /// <summary>
    /// Sent to POST /train/from-rows on the FastAPI service.
    /// </summary>
    public class SupplierRiskTrainFromRowsRequest
    {
        public List<SupplierRiskTrainingRow> rows               { get; set; } = new();

        /// <summary>
        /// When true, FastAPI merges these rows with the bundled historical dataset
        /// before retraining. Recommended when row count &lt; 500.
        /// </summary>
        public bool append_to_existing { get; set; } = true;
    }

    // ─── Train — shared response ─────────────────────────────────────────────────

    /// <summary>
    /// One split's evaluation metrics — matches FastAPI's SplitMetrics Pydantic model.
    /// </summary>
    public class SupplierRiskSplitMetrics
    {
        public double log_loss { get; set; }
        public double mse      { get; set; }
        public double mae      { get; set; }
        public double r2       { get; set; }
        public double accuracy { get; set; }
        public double auc_roc  { get; set; }
    }

    /// <summary>
    /// Response returned by all /train* endpoints on the FastAPI service.
    /// Matches FastAPI's TrainResponse Pydantic model exactly.
    /// </summary>
    public class SupplierRiskTrainResponse
    {
        public string                   message         { get; set; } = string.Empty;
        public int                      samples_trained { get; set; }
        public int                      samples_tested  { get; set; }
        public string                   model_path      { get; set; } = string.Empty;
        public string                   data_source     { get; set; } = string.Empty;
        public SupplierRiskSplitMetrics train_metrics   { get; set; } = new();
        public SupplierRiskSplitMetrics test_metrics    { get; set; } = new();
    }

    // ─── ERP aggregation (internal) ──────────────────────────────────────────────

    /// <summary>
    /// Result of the internal SQL aggregation query.
    /// Used only to build SupplierRiskPredictRequest / SupplierRiskTrainingRow payloads.
    /// </summary>
    public class SupplierRiskAggregated
    {
        public int    supplier_id      { get; set; }
        public string supplier_name    { get; set; } = string.Empty;

        /// <summary>Maps to FastAPI's supplier_price (total PO value as proxy).</summary>
        public double total_po_value   { get; set; }

        /// <summary>Maps to FastAPI's lead_time_days (average delivery days).</summary>
        public double avg_delivery_days { get; set; }

        public double claim_rate       { get; set; }
        public double on_time_rate     { get; set; }

        /// <summary>Maps to FastAPI's order_frequency.</summary>
        public int    total_orders     { get; set; }
    }
}
