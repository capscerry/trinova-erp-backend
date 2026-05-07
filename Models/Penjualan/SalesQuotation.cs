namespace trinova_erp_backend.Models.Penjualan
{
    public class SalesQuotation
    {
        // Header
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public string? QuotationNumber { get; set; }

        public DateTime QuotationDate { get; set; }

        public string? Address { get; set; }

        public string? Notes { get; set; }

        public bool? IsTaxable { get; set; }

        public bool? IsTaxIncluded { get; set; }

        public decimal? Subtotal { get; set; }

        public decimal? DiscountTotal { get; set; }

        public decimal? TaxTotal { get; set; }

        public string? Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateBy { get; set; }

        // Detail
        public List<SalesQuotationDetail> Details { get; set; } = new();
    }

    public class SalesQuotationDetail
    {
        public int QuotationId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public int UomId { get; set; }

        public decimal Price { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal? DiscountAmount { get; set; }

        public decimal? TaxPercent { get; set; }

        public decimal? TaxAmount { get; set; }
    }


}

