namespace trinova_erp_backend.Models.Persediaan
{
    public class InventoryDashboard
    {
        public int TotalProducts { get; set; }

        public int SafeStock { get; set; }

        public int CriticalStock { get; set; }

        public decimal TotalStock { get; set; }
    }
}