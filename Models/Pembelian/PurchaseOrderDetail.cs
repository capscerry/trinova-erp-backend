using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("purchase_order_detail")]
    public class PurchaseOrderDetail
    {
        [Key]
        public int purchase_order_detail_id { get; set; }

        public int purchase_order_id { get; set; }

        public string product_name { get; set; } = string.Empty;

        public int quantity { get; set; }

        public int uom_id { get; set; }

        public decimal? price { get; set; }

        public decimal? subtotal { get; set; }

        [ForeignKey("purchase_order_id")]
        public PurchaseOrder? PurchaseOrder { get; set; }
    }
}