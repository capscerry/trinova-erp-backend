namespace trinova_erp_backend.Models.Penjualan
{
    public class CategoryCustomer
    {
        public int Id { get; set; }
        public string? NamaKategori { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
