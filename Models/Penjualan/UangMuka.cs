namespace trinova_erp_backend.Models.Penjualan
{
    public class UangMuka
    {
        public int Id { get; set; }

        public string NoFaktur { get; set; } = string.Empty;

        public DateTime Tanggal { get; set; }

        public int CustomerId { get; set; }

        public string? CustomerName { get; set; }
        public string? NoPO { get; set; }
        public string? SoNumber { get; set; }

        public decimal NominalUangMuka { get; set; }

        public bool IsTaxable { get; set; }

        public bool IsTaxIncluded { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string? SyaratPembayaran { get; set; }

        public string? Alamat { get; set; }

        public string? Keterangan { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }
    }
}