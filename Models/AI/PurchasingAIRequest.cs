namespace trinova_erp_backend.Models.AI
{
    // ─── Single supplier predict ──────────────────────────────────────────────

    /// <summary>
    /// Maps to FastAPI's SupplierInput schema.
    /// POST /predict/supplier-risk
    /// </summary>
    public class AiSupplierPredictRequest
    {
        /// <summary>Optional — echoed back in the response for traceability.</summary>
        public int?   supplier_id     { get; set; }

        /// <summary>Unit price or total PO value in local currency. Must be > 0.</summary>
        public double supplier_price  { get; set; }

        /// <summary>Agreed lead time in calendar days. Minimum 1.</summary>
        public int    lead_time_days  { get; set; }

        /// <summary>Historical claim / defect rate (0.0 – 1.0).</summary>
        public double claim_rate      { get; set; }

        /// <summary>Historical on-time delivery rate (0.0 – 1.0).</summary>
        public double on_time_rate    { get; set; }

        /// <summary>Number of orders placed with this supplier per year. Minimum 1.</summary>
        public int    order_frequency { get; set; }
    }

    // ─── Batch predict ────────────────────────────────────────────────────────

    /// <summary>
    /// One supplier entry for batch prediction.
    /// Maps to FastAPI's BatchSupplierInput schema.
    /// </summary>
    public class AiBatchSupplierInput
    {
        public int?    supplier_id     { get; set; }
        public string? supplier_name   { get; set; }

        /// <summary>Must be > 0.</summary>
        public double  supplier_price  { get; set; }

        /// <summary>Minimum 1.</summary>
        public int     lead_time_days  { get; set; }

        /// <summary>0.0 – 1.0</summary>
        public double  claim_rate      { get; set; }

        /// <summary>0.0 – 1.0</summary>
        public double  on_time_rate    { get; set; }

        /// <summary>Minimum 1.</summary>
        public int     order_frequency { get; set; }
    }

    /// <summary>
    /// Sent to POST /predict/all-suppliers.
    /// Maps to FastAPI's BatchPredictRequest schema.
    /// </summary>
    public class AiBatchPredictRequest
    {
        /// <summary>One or more supplier entries to run through the XGBoost model.</summary>
        public List<AiBatchSupplierInput> suppliers { get; set; } = new();
    }

    // ─── AHP-TOPSIS rank ──────────────────────────────────────────────────────

    /// <summary>
    /// Optional custom 5×5 AHP pairwise comparison matrix.
    /// Row / column order: delay_probability, on_time_rate, claim_rate,
    ///                     supplier_price, order_frequency.
    /// Maps to FastAPI's AhpMatrixRequest schema.
    /// </summary>
    public class AiAhpMatrixRequest
    {
        public List<List<double>>? matrix { get; set; }
    }

    /// <summary>
    /// One ML-scored supplier ready for AHP-TOPSIS ranking.
    /// These fields come directly from AiSupplierPredictResult.
    /// Maps to FastAPI's RankSupplierInput schema.
    /// </summary>
    public class AiRankSupplierInput
    {
        public int?   supplier_id       { get; set; }
        public string? supplier_name    { get; set; }
        public double  supplier_price   { get; set; }
        public int     lead_time_days   { get; set; }
        public double  claim_rate       { get; set; }
        public double  on_time_rate     { get; set; }
        public int     order_frequency  { get; set; }

        /// <summary>"LOW", "MEDIUM", or "HIGH"</summary>
        public string  risk_level       { get; set; } = string.Empty;
        public double  delay_probability { get; set; }
        public int     late_probability  { get; set; }
    }

    /// <summary>
    /// Sent to POST /rank/ahp-topsis.
    /// Maps to FastAPI's RankRequest schema.
    /// </summary>
    public class AiRankRequest
    {
        /// <summary>Supplier list produced by POST /predict/all-suppliers.</summary>
        public List<AiRankSupplierInput> suppliers  { get; set; } = new();

        /// <summary>Optional custom AHP pairwise matrix. Omit to use server defaults.</summary>
        public AiAhpMatrixRequest?       ahp_matrix { get; set; }
    }

    // ─── Recommend (composite: batch-predict → rank → profiles) ──────────────

    /// <summary>
    /// Frontend-facing request to POST /api/purchasing-ai/recommend.
    /// The backend maps ERP entities into the AI request model internally.
    /// </summary>
    public class AiRecommendRequest
    {
        /// <summary>
        /// Optional custom 5×5 AHP pairwise comparison matrix.
        /// Omit to use the AI service defaults (delay_probability weighted highest).
        /// </summary>
        public List<List<double>>? ahp_matrix { get; set; }
    }

    // ─── Predict (frontend-facing thin wrapper) ───────────────────────────────

    /// <summary>
    /// Frontend-facing request to POST /api/purchasing-ai/predict.
    /// Targets a single supplier by ID — backend aggregates ERP data internally.
    /// </summary>
    public class AiPredictRequest
    {
        /// <summary>ERP supplier ID. Must be ≥ 1.</summary>
        public int supplier_id { get; set; }
    }

    // ─── Train from rows ──────────────────────────────────────────────────────

    /// <summary>
    /// One training record. Required columns match FastAPI's REQUIRED_COLUMNS.
    /// Maps to a single row in FastAPI's TrainFromRowsRequest.rows list.
    /// </summary>
    public class AiTrainingRow
    {
        public int?   supplier_id     { get; set; }
        public double supplier_price  { get; set; }
        public int    lead_time_days  { get; set; }
        public double claim_rate      { get; set; }
        public double on_time_rate    { get; set; }
        public int    order_frequency { get; set; }

        /// <summary>Ground-truth label: 0 = on-time, 1 = late.</summary>
        public int    late_delivery   { get; set; }
    }

    /// <summary>
    /// Sent to POST /train/from-rows.
    /// Maps to FastAPI's TrainFromRowsRequest schema.
    /// </summary>
    public class AiTrainFromRowsRequest
    {
        public List<AiTrainingRow> rows               { get; set; } = new();

        /// <summary>
        /// When true, FastAPI merges these rows with the bundled historical dataset
        /// before training. Recommended when row count &lt; 500.
        /// </summary>
        public bool                append_to_existing { get; set; } = true;
    }
}
