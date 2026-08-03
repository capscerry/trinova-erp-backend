namespace trinova_erp_backend.Models.AI
{
    // ─── Unified API envelope ─────────────────────────────────────────────────

    /// <summary>
    /// Standard envelope returned from every PurchasingAIController endpoint.
    /// Keeps the frontend contract consistent regardless of success or failure.
    /// </summary>
    public class AiApiResponse<T>
    {
        public bool    success { get; set; }
        public string  message { get; set; } = string.Empty;
        public T?      data    { get; set; }

        public static AiApiResponse<T> Ok(T data, string message = "Success") =>
            new() { success = true, message = message, data = data };

        public static AiApiResponse<T> Fail(string message) =>
            new() { success = false, message = message, data = default };
    }

    // ─── Single supplier predict ──────────────────────────────────────────────

    /// <summary>
    /// Response from POST /predict/supplier-risk.
    /// Maps to FastAPI's SupplierRiskResponse schema.
    /// </summary>
    public class AiSupplierPredictResponse
    {
        public int?   supplier_id       { get; set; }

        /// <summary>"LOW", "MEDIUM", or "HIGH"</summary>
        public string risk_level        { get; set; } = string.Empty;

        /// <summary>Probability of a late delivery (0.0 – 1.0).</summary>
        public double delay_probability { get; set; }

        /// <summary>delay_probability expressed as a percentage (0 – 100).</summary>
        public int    late_probability  { get; set; }
    }

    // ─── Batch predict ────────────────────────────────────────────────────────

    /// <summary>
    /// ML result for a single supplier from POST /predict/all-suppliers.
    /// Maps to FastAPI's SupplierPredictResult schema.
    /// </summary>
    public class AiSupplierPredictResult
    {
        public int?    supplier_id       { get; set; }
        public string? supplier_name     { get; set; }
        public double  supplier_price    { get; set; }
        public int     lead_time_days    { get; set; }
        public double  claim_rate        { get; set; }
        public double  on_time_rate      { get; set; }
        public int     order_frequency   { get; set; }

        /// <summary>"LOW", "MEDIUM", or "HIGH"</summary>
        public string  risk_level        { get; set; } = string.Empty;
        public double  delay_probability { get; set; }
        public int     late_probability  { get; set; }
    }

    /// <summary>
    /// Response from POST /predict/all-suppliers — raw ML scores, no ranking.
    /// Maps to FastAPI's BatchPredictResponse schema.
    /// </summary>
    public class AiBatchPredictResponse
    {
        public int                          total   { get; set; }
        public List<AiSupplierPredictResult> results { get; set; } = new();
    }

    // ─── AHP-TOPSIS rank ──────────────────────────────────────────────────────

    /// <summary>
    /// One ranked supplier — all ML fields plus TOPSIS score and rank.
    /// Maps to FastAPI's RankedSupplierResult schema.
    /// </summary>
    public class AiRankedSupplierResult
    {
        public int?    supplier_id       { get; set; }
        public string? supplier_name     { get; set; }
        public double  supplier_price    { get; set; }
        public int     lead_time_days    { get; set; }
        public double  claim_rate        { get; set; }
        public double  on_time_rate      { get; set; }
        public int     order_frequency   { get; set; }

        /// <summary>"LOW", "MEDIUM", or "HIGH"</summary>
        public string  risk_level        { get; set; } = string.Empty;
        public double  delay_probability { get; set; }
        public int     late_probability  { get; set; }

        public double  topsis_score      { get; set; }
        public int     topsis_rank       { get; set; }
    }

    /// <summary>
    /// Response from POST /rank/ahp-topsis.
    /// Maps to FastAPI's RankResponse schema.
    /// Suppliers are ordered by topsis_rank ascending (rank 1 = best supplier).
    /// </summary>
    public class AiRankResponse
    {
        public int                              total             { get; set; }

        /// <summary>AHP consistency ratio (CR). Should be ≤ 0.10.</summary>
        public double                           consistency_ratio { get; set; }

        /// <summary>
        /// Derived criterion weights (sum ≈ 1.0).
        /// Keys: delay_probability, on_time_rate, claim_rate, supplier_price, order_frequency.
        /// </summary>
        public Dictionary<string, double>       ahp_weights       { get; set; } = new();

        public List<AiRankedSupplierResult>     ranked_suppliers  { get; set; } = new();
    }

    // ─── Recommendation profiles ──────────────────────────────────────────────

    /// <summary>
    /// The top-ranked supplier for a single purchasing profile, derived from
    /// the AHP-TOPSIS ranked_suppliers list — no additional calculation.
    /// </summary>
    public class AiSupplierRecommendationProfile
    {
        /// <summary>"Balanced", "High Urgency", "Budget Priority", or "Quality Focus".</summary>
        public string  profile         { get; set; } = string.Empty;
        public int?    supplier_id     { get; set; }
        public string? supplier_name   { get; set; }
        public double  topsis_score    { get; set; }
        public int     topsis_rank     { get; set; }
        public double  on_time_rate    { get; set; }
        public double  claim_rate      { get; set; }
        public int     lead_time_days  { get; set; }
        public double  supplier_price  { get; set; }
        public int     order_frequency { get; set; }
        public string  risk_level      { get; set; } = string.Empty;
    }

    /// <summary>
    /// Complete unified recommendation returned by POST /api/purchasing-ai/recommend.
    /// Every purchasing screen must consume this object as-is.
    /// </summary>
    public class AiRecommendationResult
    {
        /// <summary>Full AHP-TOPSIS ranked list — the authoritative supplier ordering.</summary>
        public AiRankResponse ranking { get; set; } = new();

        /// <summary>
        /// Per-profile best suppliers derived purely from ranked_suppliers.
        /// Keys: "Balanced", "High Urgency", "Budget Priority", "Quality Focus".
        /// </summary>
        public Dictionary<string, AiSupplierRecommendationProfile> profiles { get; set; } = new();
    }

    // ─── Training responses ───────────────────────────────────────────────────

    /// <summary>
    /// Evaluation metrics for one data split or CV fold.
    /// Maps to FastAPI's SplitMetrics schema.
    /// </summary>
    public class AiSplitMetrics
    {
        public double accuracy  { get; set; }
        public double precision { get; set; }
        public double recall    { get; set; }
        public double f1_score  { get; set; }
        public double auc_roc   { get; set; }
        public double log_loss  { get; set; }
    }

    /// <summary>
    /// Metrics for one TimeSeriesSplit CV fold.
    /// Maps to FastAPI's FoldMetrics schema.
    /// </summary>
    public class AiFoldMetrics
    {
        public int    fold      { get; set; }
        public double accuracy  { get; set; }
        public double precision { get; set; }
        public double recall    { get; set; }
        public double f1_score  { get; set; }
        public double auc_roc   { get; set; }
        public double log_loss  { get; set; }
    }

    /// <summary>
    /// Aggregated cross-validation results on the training set.
    /// Maps to FastAPI's CVResults schema.
    /// </summary>
    public class AiCVResults
    {
        public List<AiFoldMetrics> fold_metrics  { get; set; } = new();

        public double? avg_accuracy  { get; set; }
        public double? avg_precision { get; set; }
        public double? avg_recall    { get; set; }
        public double? avg_f1_score  { get; set; }
        public double? avg_auc_roc   { get; set; }
        public double? avg_log_loss  { get; set; }
        public double? std_accuracy  { get; set; }
        public double? std_precision { get; set; }
        public double? std_recall    { get; set; }
        public double? std_f1_score  { get; set; }
        public double? std_auc_roc   { get; set; }
        public double? std_log_loss  { get; set; }
    }

    /// <summary>
    /// Response from all /train* endpoints.
    /// Maps to FastAPI's TrainResponse schema.
    /// </summary>
    public class AiTrainResponse
    {
        public string message         { get; set; } = string.Empty;

        /// <summary>"time_based_80_20" — first 80 % training, last 20 % testing.</summary>
        public string split_method    { get; set; } = string.Empty;

        public int    samples_trained { get; set; }
        public int    samples_tested  { get; set; }

        /// <summary>Number of XGBoost trees after early stopping.</summary>
        public int    best_round      { get; set; }

        public string model_path      { get; set; } = string.Empty;
        public string data_source     { get; set; } = string.Empty;

        public AiSplitMetrics train_metrics { get; set; } = new();
        public AiSplitMetrics test_metrics  { get; set; } = new();
        public AiCVResults    cv_results    { get; set; } = new();
    }

    // ─── Health check ─────────────────────────────────────────────────────────

    /// <summary>
    /// Response from GET /api/purchasing-ai/health.
    /// Exposes AI service connectivity status and latency to the frontend.
    /// </summary>
    public class AiHealthResponse
    {
        public bool   connected { get; set; }
        public string service   { get; set; } = "Purchasing AI";
        public string url       { get; set; } = string.Empty;

        /// <summary>Round-trip latency in milliseconds. Null when unreachable.</summary>
        public long?  latency_ms { get; set; }

        /// <summary>Reason for failure when connected is false.</summary>
        public string? error    { get; set; }
    }
}
