using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Models
{
    [Table("purchase_order_detail")]
    public class PurchaseOrderDetail
    {
        [Key]
        public int purchase_order_detail_id { get; set; }

        public int purchase_order_id { get; set; }

        public int product_id { get; set; }

        public int quantity { get; set; }

        public int uom_id { get; set; }

        public decimal? price { get; set; }

        public decimal? tax_percentage { get; set; }

        public decimal? tax_amount { get; set; }

        public decimal? subtotal { get; set; }

        [ForeignKey("purchase_order_id")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        // [ForeignKey("product_id")]
        // public MasterProduct? MasterProduct { get; set; }
    }
}