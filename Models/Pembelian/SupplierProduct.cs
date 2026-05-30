using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("supplier_products")]
    public class SupplierProduct
    {
        [Key]
        public int supplier_product_id { get; set; }

        public int supplier_id { get; set; }

        public int product_id { get; set; }

        public decimal supplier_price { get; set; }

        public int available_stock { get; set; }

        public int lead_time_days { get; set; }

        public bool is_available { get; set; }

        public DateTime? created_at { get; set; }

        // ─── JOIN RESULT ─────────────────────

        [NotMapped]
        public string? product_name { get; set; }

        [NotMapped]
        public string? supplier_name { get; set; }

        [NotMapped]
        public int? uom_id { get; set; }
    }
}