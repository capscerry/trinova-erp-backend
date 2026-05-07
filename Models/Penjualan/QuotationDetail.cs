namespace trinova_erp_backend.Models.Penjualan
{
    public class QuotationDetail
    {
        public int Id { get; set; }

        public int QuotationId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public int UomId { get; set; }

        public decimal Price { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        public DateTime CreatedDate { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateBy { get; set; }
    }
}
