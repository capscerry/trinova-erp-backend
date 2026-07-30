using System.Text.Json.Serialization;

namespace trinova_erp_backend.Models.Persediaan
{
    public class ForecastDatasetItem
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("tahun")]
        public int Tahun { get; set; }

        [JsonPropertyName("bulan")]
        public int Bulan { get; set; }

        [JsonPropertyName("total_usage")]
        public double TotalUsage { get; set; }
    }
}