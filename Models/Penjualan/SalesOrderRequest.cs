namespace trinova_erp_backend.Models.Penjualan
{

    public class SalesOrderRequest
    {
        public SalesOrderHeader Header { get; set; }
        public List<SalesOrderDetail> Detail { get; set; }
    }
    public class SalesOrderHeader
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string SoNumber { get; set; }
        public DateTime? TanggalKirim { get; set; }
        public DateTime? SoDate { get; set; }
        public string? PoNumber { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? DiscountTotal { get; set; }
        public decimal? TaxTotal { get; set; }
        public Boolean? IsTaxAble { get; set; }
        public Boolean IsTaxIncluded { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public int? QuotationId { get; set; }


    }
    public class SalesOrderDetail { 
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int ProductQty { get; set; }
        public decimal ProductPrice { get; set; }
        public int DiscountPercent { get; set; }
        public decimal TotalPrice {  get; set; }
        public int? WareHouseId { get; set; }
        public int UomId { get; set; }
    }

}
