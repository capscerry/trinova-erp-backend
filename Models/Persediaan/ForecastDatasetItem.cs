using System.Text.Json.Serialization;

namespace trinova_erp_backend.Models.Persediaan
{
    /// <summary>
    /// One row in the forecast training dataset returned by
    /// GET /api/inventory/forecast-dataset.
    ///
    /// Each row represents the total OUT-type stock usage for a single
    /// product in a single calendar month.  The Python AI service
    /// (trinova-ai) consumes this endpoint to build its demand-forecast
    /// model without connecting to SQL Server directly.
    ///
    /// Field names are snake_case to match the pandas DataFrame column
    /// names the AI service expects:
    ///   product_id, product_name, tahun, bulan, total_usage
    /// </summary>
    public class ForecastDatasetItem
    {
        [JsonPropertyName("product_id")]
        public int ProductId { get; set; }

        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Calendar year of the usage period (e.g. 2024).</summary>
        [JsonPropertyName("tahun")]
        public int Tahun { get; set; }

        /// <summary>Calendar month of the usage period (1–12).</summary>
        [JsonPropertyName("bulan")]
        public int Bulan { get; set; }

        /// <summary>
        /// Sum of OUT-type stock transactions for this product/month.
        /// Represents demand consumed from inventory.
        /// </summary>
        [JsonPropertyName("total_usage")]
        public double TotalUsage { get; set; }
    }
}
