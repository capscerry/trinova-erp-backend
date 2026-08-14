using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("purchase_return")]
    public class PurchaseReturn
    {
        // ── NOT persisted to purchase_return — held in purchase_return_item ──
        // Populated from the POST body so the usecase can insert child rows
        // and use the correct qty_return for stock operations.
        [NotMapped]
        public List<PurchaseReturnItem> return_items { get; set; } = new();

        [Key]
        public int purchase_return_id { get; set; }

        public int goods_receipt_id { get; set; }

        public string purchase_return_number { get; set; } = "";

        public DateTime return_date { get; set; }

        public string supplier_name { get; set; } = "";

        public string purchase_order_number { get; set; } = "";

        public decimal total_amount { get; set; }

        public string settlement_option { get; set; } = "";

        public string notes { get; set; } = "";

        public string status { get; set; } = "";

        public string closing_condition { get; set; } = "";

        public string transaction_name { get; set; } = "";

        public string transaction_detail { get; set; } = "";

        public DateTime? created_at { get; set; }
    }
}
