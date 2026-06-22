namespace trinova_erp_backend.Models.DTO
{
    public class QuotationHeaderDTO
    {
        public int Id { get; set; }
        public string QuotationNumber { get; set; }
        public string CustomerName { get; set; }
        public DateTime QuotationDate { get; set; }
        public string? Notes { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }

    }
}
