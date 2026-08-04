namespace trinova_erp_backend.Models.Penjualan
{
    public class SalesKpiNumbers
    {
        public int TotalSalesOrder { get; set; }
        public int TotalSalesOrderPrevMonth { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalRevenuePrevMonth { get; set; }
        public decimal OutstandingReceivable { get; set; }
        public double FulfillmentRate { get; set; }
        public int SalesReturnCount { get; set; }
        public int SalesReturnPrevMonth { get; set; }
    }

    public class MonthPointDTO
    {
        public string Month { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class SOStatusCountDTO
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CustomerRevenueDTO
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class CategoryRevenueDTO
    {
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class SalesKpiCharts
    {
        public List<MonthPointDTO> RevenueTrend { get; set; } = new();
        public List<MonthPointDTO> SalesVolumeTrend { get; set; } = new();
        public List<SOStatusCountDTO> SoStatusDistribution { get; set; } = new();
        public List<CustomerRevenueDTO> TopCustomersByRevenue { get; set; } = new();
        public List<CategoryRevenueDTO> CategoryDistribution { get; set; } = new();
        public List<MonthPointDTO> OutstandingTrend { get; set; } = new();
        public List<MonthPointDTO> ReturnTrend { get; set; } = new();
    }

    public class SalesFunnelStepDTO
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CustomerLeaderboardRowDTO
    {
        public int Rank { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int InvoiceCount { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class SalesKpiResult
    {
        public SalesKpiNumbers Kpis { get; set; } = new();
        public SalesKpiCharts Charts { get; set; } = new();
        public List<SalesFunnelStepDTO> Funnel { get; set; } = new();
        public List<CustomerLeaderboardRowDTO> Leaderboard { get; set; } = new();
    }

    /// <summary>
    /// Query params for the Sales KPI endpoint. From/To/PrevFrom/PrevTo are
    /// computed by the frontend (same getDateBounds logic as before) so the
    /// backend doesn't need to duplicate "what does last6m mean" -- it only
    /// aggregates whatever range it's given.
    /// </summary>
    public class SalesKpiQuery
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime PrevFrom { get; set; }
        public DateTime PrevTo { get; set; }
        public int? CustomerId { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
    }
}
