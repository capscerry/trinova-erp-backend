using trinova_erp_backend.Models.AI;

namespace trinova_erp_backend.Services.PurchasingAI
{
    /// <summary>
    /// Abstracts all communication with the Railway Purchasing AI service.
    /// Controllers must depend on this interface, never on HttpClient directly.
    ///
    /// Every method is gracefully degraded:
    ///   - AI unavailability never causes HTTP 500.
    ///   - Failures are logged and surfaced through AiApiResponse.
    /// </summary>
    public interface IPurchasingAIService
    {
        /// <summary>
        /// Aggregates live ERP data for ALL active suppliers, runs batch ML prediction,
        /// then ranks with AHP-TOPSIS and derives the four purchasing profiles.
        ///
        /// This is the primary endpoint for all purchasing recommendation screens.
        /// Pass a custom AHP matrix to override the default weights.
        /// </summary>
        Task<AiApiResponse<AiRecommendationResult>> GetRecommendationAsync(
            List<List<double>>? ahpMatrix = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aggregates live ERP data for a single supplier and runs the XGBoost
        /// risk classifier. Returns LOW / MEDIUM / HIGH + delay probability.
        /// </summary>
        Task<AiApiResponse<AiSupplierPredictResponse>> PredictSupplierRiskAsync(
            int supplierId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aggregates live ERP data for ALL active suppliers and runs batch
        /// ML prediction (no ranking). Call GetRecommendationAsync for the full
        /// ranked result.
        /// </summary>
        Task<AiApiResponse<AiBatchPredictResponse>> BatchPredictAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends pre-scored supplier entries to POST /rank/ahp-topsis and returns
        /// the AHP-TOPSIS ranked list. Use when ML results are already available
        /// and only ranking is needed.
        /// </summary>
        Task<AiApiResponse<AiRankResponse>> RankSuppliersAsync(
            AiRankRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Probes the AI service health endpoint and measures round-trip latency.
        /// Never throws — always returns a response with connected = true/false.
        /// </summary>
        Task<AiHealthResponse> CheckHealthAsync(
            CancellationToken cancellationToken = default);
    }
}
