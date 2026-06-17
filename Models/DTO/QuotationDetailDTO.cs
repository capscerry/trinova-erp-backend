namespace trinova_erp_backend.Models.DTO
{
    public class QuotationDetailDTO
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }

        public int Quantity { get; set; }
        public int UomId { get; set; }
        public string? UomCode { get; set; }
        public decimal Price { get; set; }

    }
}
