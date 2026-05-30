namespace trinova_erp_backend.Models
{
    public class SupplierProductImport
    {
        public int supplier_id { get; set; }

        public int product_id { get; set; }

        public decimal supplier_price { get; set; }

        public int available_stock { get; set; }

        public int lead_time_days { get; set; }
    }
}