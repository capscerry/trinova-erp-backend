namespace trinova_erp_backend.Models.Persediaan
{
    public class ForecastResult
    {
        public int product_id { get; set; }

        public string product_name { get; set; } = string.Empty;

        public decimal total_usage { get; set; }

        public decimal forecast_next_month { get; set; }

        public decimal current_stock { get; set; }

        public string recommendation { get; set; } = string.Empty;
    }
}