namespace trinova_erp_backend.Models.Penjualan
{
    public class SalesInvoice
    {
        public SalesInvoiceHeader Header { get; set; } = new();
        public List<SalesInvoiceDetail> Detail { get; set; } = new();
    }

    public class SalesInvoiceHeader
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public int? DeliveryOrderId { get; set; }
        public string? DeliveryOrderNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Draft";
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal DownPaymentAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SalesInvoiceDetail
    {
        public int Id { get; set; }
        public int SalesInvoiceId { get; set; }
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public int? UomId { get; set; }
        public string? UomName { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmount
        {
            get => Discount;
            set => Discount = value;
        }
        public decimal Tax { get; set; }
        public decimal TaxAmount
        {
            get => Tax;
            set => Tax = value;
        }
        public decimal Subtotal { get; set; }
        public int? SalesOrderItemId { get; set; }
        public int? DeliveryOrderItemId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
