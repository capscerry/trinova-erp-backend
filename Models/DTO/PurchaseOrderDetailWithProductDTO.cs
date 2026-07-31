namespace trinova_erp_backend.Models.DTO
{
    public class PurchaseOrderDetailWithProductDTO
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public string? UomCode { get; set; }
        public decimal Price { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal Subtotal { get; set; }
    }
}
