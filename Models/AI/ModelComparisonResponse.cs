using System.Text.Json.Serialization;

namespace trinova_erp_backend.Models.AI
{
    public class ModelComparisonResponse
    {
        [JsonPropertyName("evaluation_method")]
        public string EvaluationMethod { get; set; } = string.Empty;

        [JsonPropertyName("training_period")]
        public string TrainingPeriod { get; set; } = string.Empty;

        [JsonPropertyName("testing_period")]
        public string TestingPeriod { get; set; } = string.Empty;

        [JsonPropertyName("total_products")]
        public int TotalProducts { get; set; }

        [JsonPropertyName("products_evaluated")]
        public int ProductsEvaluated { get; set; }

        [JsonPropertyName("best_model")]
        public string BestModel { get; set; } = string.Empty;

        [JsonPropertyName("models")]
        public List<ModelComparisonResult> Models { get; set; } = new();
    }

    public class ModelComparisonResult
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("mae")]
        public double Mae { get; set; }

        [JsonPropertyName("rmse")]
        public double Rmse { get; set; }

        [JsonPropertyName("r2")]
        public double R2 { get; set; }

        [JsonPropertyName("products_evaluated")]
        public int ProductsEvaluated { get; set; }
    }
}