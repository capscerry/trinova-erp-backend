namespace trinova_erp_backend.Models.Persediaan
{
    public class InventoryDashboard
    {
        public int TotalProducts { get; set; }

        public int SafeStock { get; set; }

        public int CriticalStock { get; set; }

        public decimal TotalStock { get; set; }

        public InventoryAiSummary AiSummary { get; set; } = new();
    }

    public class InventoryAiSummary
    {
        public string ForecastMonth { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }

        public int ForecastedProducts { get; set; }

        public decimal AverageForecast { get; set; }

        public int NeedRestock { get; set; }

        public ForecastProduct HighestDemand { get; set; } = new();

        public ForecastProduct LowestDemand { get; set; } = new();

        public List<ForecastProduct> TopForecastProducts { get; set; } = new();
    }

    public class ForecastProduct
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Forecast { get; set; }
    }
}