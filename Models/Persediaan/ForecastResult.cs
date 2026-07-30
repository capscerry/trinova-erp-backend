using System.Text.Json.Serialization;

namespace trinova_erp_backend.Models.Persediaan
{
    public class ForecastResult
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("forecast_month")]
        public string ForecastMonth { get; set; } = string.Empty;

        [JsonPropertyName("last_training_period")]
        public string LastTrainingPeriod { get; set; } = string.Empty;

        [JsonPropertyName("historical_records")]
        public int HistoricalRecords { get; set; }

        [JsonPropertyName("forecast_next_month")]
        public double ForecastNextMonth { get; set; }

        [JsonPropertyName("generated_at")]
        public DateTime GeneratedAt { get; set; }

    }
}