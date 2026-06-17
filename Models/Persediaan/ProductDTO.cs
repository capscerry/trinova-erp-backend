namespace trinova_erp_backend.Models.Persediaan
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Uom { get; set; }
        public int UomId { get; set; }

        
    }
}
