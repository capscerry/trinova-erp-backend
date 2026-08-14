using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("purchase_return_item")]
    public class PurchaseReturnItem
    {
        [Key]
        public int purchase_return_item_id { get; set; }

        public int purchase_return_id { get; set; }

        public int product_id { get; set; }

        public string product_name { get; set; } = "";

        public int qty_return { get; set; }

        public decimal unit_price { get; set; }

        public decimal subtotal { get; set; }
    }
}
