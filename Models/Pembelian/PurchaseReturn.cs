using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("purchase_return")]
    public class PurchaseReturn
    {
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
