namespace trinova_erp_backend.Models.DTO
{
    public class SalesOrderDetailDTO
    {
        public string? SoNumber { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime SoDate { get; set; }
        public DateTime TanggalKirim { get; set; }
        public string? PoNumber { get; set; }
        public string? Address { get; set; }
        public string? Keterangan { get; set; }
        public decimal Total { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public bool IsTaxAble { get; set; }
        public int? QuotationId { get; set; }
        public string? QuotationNumber { get; set; }
        public List<SalesOrderProductDetail> Detail { get; set; } = new();
    }

    public class SalesOrderProductDetail
    {
        public string? ProductName { get; set; }
        public int ProductId { get; set; }
        public int ProductQty { get; set; }
        public decimal ProductPrice { get; set; }
        public int ProductDiscount { get; set; }
        public decimal TotalPrice { get; set; }
        public int? WareHouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int? UomId { get; set; }
        public string? UomCode { get; set; }
    }
}