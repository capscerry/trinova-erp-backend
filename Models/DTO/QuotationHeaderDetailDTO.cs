namespace trinova_erp_backend.Models.DTO
{
    public class QuotationHeaderDetailDTO
    {
        public QuotationHeaderDTO? Header { get; set; }
        public List<QuotationDetailDTO> Detail { get; set; } = new();
    }
}
