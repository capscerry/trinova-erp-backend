namespace trinova_erp_backend.Models.Penjualan
{
    public class SalesReturn
    {
        public SalesReturnHeaderDTO Header { get; set; } = new();
        public List<SalesReturnDetailDTO> Detail { get; set; } = new();
    }

    public class SalesReturnHeaderDTO
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int DeliveryOrderId { get; set; }
        public string? DoNumber { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SoNumber { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Completed";
        public DateTime? CreatedAt { get; set; }
    }

    public class SalesReturnDetailDTO
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public decimal Qty { get; set; }
        public int? UomId { get; set; }
        public string? UomName { get; set; }
        public string? Reason { get; set; }
    }
}
