namespace trinova_erp_backend.Models.Penjualan
{
    public class SalesStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
