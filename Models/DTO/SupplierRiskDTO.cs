namespace trinova_erp_backend.Models.DTO
{

    /// <summary>
    /// Request body sent to POST /predict/supplier-risk on the FastAPI service.
    /// Field names match FastAPI's SupplierInput schema.
    /// </summary>
    public class SupplierRiskPredictRequest
    {
        /// <summary>Optional — for traceability only, not used as a feature.</summary>
        public int?   supplier_id      { get; set; }

        public double supplier_price   { get; set; }
        public int    lead_time_days   { get; set; }
        public double claim_rate       { get; set; }
        public double on_time_rate     { get; set; }
        public int    order_frequency  { get; set; }
    }

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
    /// Metrics for a single TimeSeriesSplit CV fold — matches FastAPI's FoldMetrics model.
    /// </summary>
    public class SupplierRiskFoldMetrics
    {
        public int    fold     { get; set; }
        public double log_loss { get; set; }
        public double mse      { get; set; }
        public double mae      { get; set; }
        public double r2       { get; set; }
        public double accuracy { get; set; }
        public double auc_roc  { get; set; }
    }

    /// <summary>
    /// Aggregated TimeSeriesSplit cross-validation results — matches FastAPI's CVResults model.
    ///
    /// CV is run on the training set only (first 80% of data in chronological order).
    /// Each fold expands forward in time so validation rows always come after training
    /// rows, preventing any future-data leakage.
    /// </summary>
    public class SupplierRiskCVResults
    {
        public List<SupplierRiskFoldMetrics> fold_metrics  { get; set; } = new();

        /// <summary>Mean log-loss across all folds. Null when CV was skipped.</summary>
        public double? avg_log_loss  { get; set; }

        /// <summary>Mean MSE across all folds.</summary>
        public double? avg_mse       { get; set; }

        /// <summary>Mean MAE across all folds.</summary>
        public double? avg_mae       { get; set; }

        /// <summary>Mean R² across all folds.</summary>
        public double? avg_r2        { get; set; }

        /// <summary>Mean accuracy across all folds.</summary>
        public double? avg_accuracy  { get; set; }

        /// <summary>Mean AUC-ROC across all folds.</summary>
        public double? avg_auc_roc   { get; set; }
    }

    /// <summary>
    /// Response returned by all /train* endpoints on the FastAPI service.
    /// Matches FastAPI's TrainResponse Pydantic model exactly.
    /// </summary>
    public class SupplierRiskTrainResponse
    {
        public string message         { get; set; } = string.Empty;

        /// <summary>
        /// Always "time_based_80_20" — the first 80% of rows (chronologically)
        /// are used for training, the last 20% for testing. No random shuffling.
        /// </summary>
        public string split_method    { get; set; } = string.Empty;

        public int    samples_trained { get; set; }
        public int    samples_tested  { get; set; }

        /// <summary>Number of XGBoost trees used after early stopping.</summary>
        public int    best_round      { get; set; }

        public string model_path      { get; set; } = string.Empty;
        public string data_source     { get; set; } = string.Empty;

        public SupplierRiskSplitMetrics train_metrics { get; set; } = new();
        public SupplierRiskSplitMetrics test_metrics  { get; set; } = new();

        /// <summary>
        /// TimeSeriesSplit cross-validation results computed on the training set.
        /// Use avg_accuracy / avg_auc_roc to compare model iterations without
        /// touching the held-out test set.
        /// </summary>
        public SupplierRiskCVResults    cv_results    { get; set; } = new();
    }

    // ─── Batch predict ───────────────────────────────────────────────────────────

    /// <summary>
    /// One supplier entry sent inside POST /predict/all-suppliers.
    /// ERP field mapping mirrors SupplierRiskPredictRequest:
    ///   total_po_value    → supplier_price
    ///   avg_delivery_days → lead_time_days
    ///   total_orders      → order_frequency
    /// </summary>
    public class BatchSupplierInput
    {
        public int?   supplier_id      { get; set; }
        public string? supplier_name   { get; set; }
        public double supplier_price   { get; set; }
        public int    lead_time_days   { get; set; }
        public double claim_rate       { get; set; }
        public double on_time_rate     { get; set; }
        public int    order_frequency  { get; set; }
    }

    /// <summary>Sent to POST /predict/all-suppliers.</summary>
    public class BatchPredictRequest
    {
        public List<BatchSupplierInput> suppliers { get; set; } = new();
    }

    /// <summary>
    /// ML result for one supplier as returned by POST /predict/all-suppliers.
    /// Carries both the original ERP features and the XGBoost output —
    /// this is the payload you feed directly into POST /rank/ahp-topsis.
    /// </summary>
    public class SupplierPredictResult
    {
        public int?   supplier_id       { get; set; }
        public string? supplier_name    { get; set; }
        public double supplier_price    { get; set; }
        public int    lead_time_days    { get; set; }
        public double claim_rate        { get; set; }
        public double on_time_rate      { get; set; }
        public int    order_frequency   { get; set; }

        // XGBoost output
        public string risk_level        { get; set; } = string.Empty;
        public double delay_probability { get; set; }
        public int    late_probability  { get; set; }
    }

    /// <summary>
    /// Response from POST /predict/all-suppliers — raw ML scores, no ranking.
    /// </summary>
    public class BatchPredictResponse
    {
        public int                          total   { get; set; }
        public List<SupplierPredictResult>  results { get; set; } = new();
    }

    // ─── AHP-TOPSIS rank ─────────────────────────────────────────────────────────

    /// <summary>
    /// Optional custom 5×5 AHP pairwise comparison matrix.
    /// Leave null to use the FastAPI default matrix.
    /// Row/col order: delay_probability, on_time_rate, claim_rate,
    ///                supplier_price, order_frequency.
    /// </summary>
    public class AhpMatrixRequest
    {
        public List<List<double>>? matrix { get; set; }
    }

    /// <summary>Sent to POST /rank/ahp-topsis.</summary>
    public class RankRequest
    {
        /// <summary>
        /// The supplier list from BatchPredictResponse.results —
        /// pass it here unchanged.
        /// </summary>
        public List<SupplierPredictResult> suppliers  { get; set; } = new();

        /// <summary>Optional custom AHP matrix. Omit to use server defaults.</summary>
        public AhpMatrixRequest            ahp_matrix { get; set; } = new();
    }

    /// <summary>
    /// One ranked supplier — all ML fields plus TOPSIS score and rank.
    /// </summary>
    public class RankedSupplierResult
    {
        public int?   supplier_id       { get; set; }
        public string? supplier_name    { get; set; }
        public double supplier_price    { get; set; }
        public int    lead_time_days    { get; set; }
        public double claim_rate        { get; set; }
        public double on_time_rate      { get; set; }
        public int    order_frequency   { get; set; }
        public string risk_level        { get; set; } = string.Empty;
        public double delay_probability { get; set; }
        public int    late_probability  { get; set; }

        // TOPSIS output
        public double topsis_score      { get; set; }
        public int    topsis_rank       { get; set; }
    }

    /// <summary>
    /// Response from POST /rank/ahp-topsis.
    /// Suppliers are sorted by topsis_rank ascending (rank 1 = best supplier).
    /// </summary>
    public class RankResponse
    {
        public int                              total             { get; set; }
        public double                           consistency_ratio { get; set; }
        public Dictionary<string, double>       ahp_weights       { get; set; } = new();
        public List<RankedSupplierResult>       ranked_suppliers  { get; set; } = new();
    }

    // ─── ERP aggregation (internal) ──────────────────────────────────────────────

    /// <summary>
    /// Result of the internal SQL aggregation query.
    /// Used only to build SupplierRiskPredictRequest / SupplierRiskTrainingRow payloads.
    /// </summary>
    public class SupplierRiskAggregated
    {
        public int    supplier_id       { get; set; }
        public string supplier_name     { get; set; } = string.Empty;

        /// <summary>Maps to FastAPI's supplier_price (total PO value as proxy).</summary>
        public double total_po_value    { get; set; }

        /// <summary>Maps to FastAPI's lead_time_days (average delivery days).</summary>
        public double avg_delivery_days { get; set; }

        public double claim_rate        { get; set; }
        public double on_time_rate      { get; set; }

        /// <summary>Maps to FastAPI's order_frequency.</summary>
        public int    total_orders      { get; set; }
    }
}
