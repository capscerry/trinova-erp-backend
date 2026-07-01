namespace trinova_erp_backend.Models.Penjualan
{
    public class SalesDashboard
    {
        public decimal TotalSalesOrder { get; set; }
        public int SalesOrderCount { get; set; }
        public decimal TotalInvoice { get; set; }
        public int InvoiceCount { get; set; }
        public decimal OutstandingInvoice { get; set; }
        public decimal TotalReceipt { get; set; }
        public int ReceiptCount { get; set; }
        public int DeliveryCount { get; set; }
        public int CustomerCount { get; set; }
        public List<SalesDashboardOrderItem> RecentSalesOrders { get; set; } = new();
        public List<SalesDashboardInvoiceItem> RecentInvoices { get; set; } = new();
        public List<SalesDashboardActivityItem> RecentActivities { get; set; } = new();
        public List<SalesDashboardUpcomingActivityItem> UpcomingActivities { get; set; } = new();
    }

    public class SalesDashboardOrderItem
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SalesDashboardInvoiceItem
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SalesDashboardActivityItem
    {
        public long Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RefTable { get; set; }
        public long? RefId { get; set; }
        public string? RefNumber { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class SalesDashboardUpcomingActivityItem
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RefTable { get; set; }
        public long? RefId { get; set; }
        public string? RefNumber { get; set; }
        public DateTime ActivityDate { get; set; }
        public string Priority { get; set; } = "normal";
    }
}
