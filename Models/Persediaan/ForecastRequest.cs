using System.Text.Json.Serialization;

namespace trinova_erp_backend.Models.Persediaan
{
    public class ForecastRequest
    {
        [JsonPropertyName("items")]
        public List<ForecastDatasetItem> Items { get; set; } = new();
    }
}   