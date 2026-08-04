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

        // Computed from open Purchase Orders (Approved/Completed) minus what has
        // already been received via Goods Receipt for this supplier+product.
        // available_stock itself is never touched by PO/GR -- it always reflects
        // the supplier's last uploaded catalog snapshot.
        [NotMapped]
        public int reserved_quantity { get; set; }

        [NotMapped]
        public int available_to_order { get; set; }
    }
}