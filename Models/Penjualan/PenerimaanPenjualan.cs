namespace trinova_erp_backend.Models.Penjualan
{
    public class PenerimaanPenjualan
    {
        public int Id { get; set; }
        public string NoBukti { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int BankId { get; set; }
        public string? BankName { get; set; }
        public decimal NilaiPembayaran { get; set; }
        public DateTime TanggalBayar { get; set; }
        public int? UangMukaId { get; set; }
        public int? SalesOrderId { get; set; }
        public string? Status { get; set; }
    }

    public class BankDTO
    {
        public int Id { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
    }
}
