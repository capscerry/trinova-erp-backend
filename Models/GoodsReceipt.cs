using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("goods_receipt")]
    public class GoodsReceipt
    {
        [Key]
        public int goods_receipt_id { get; set; }

        public int purchase_order_id { get; set; }

        public string? receipt_number { get; set; }

        public DateTime? receipt_date { get; set; }

        public string? received_by { get; set; }

        public string? status { get; set; }

        public DateTime? created_at { get; set; }

        [ForeignKey("purchase_order_id")]
        public PurchaseOrder? PurchaseOrder { get; set; }
    }
}