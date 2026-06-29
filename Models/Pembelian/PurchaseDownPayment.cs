using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("purchase_down_payment")]
    public class PurchaseDownPayment
    {
        [Key]
        public int purchase_down_payment_id { get; set; }

        public string dp_number { get; set; } = string.Empty;

        public int purchase_order_id { get; set; }

        public int supplier_id { get; set; }

        public DateTime? payment_date { get; set; }

        public string? payment_type { get; set; }

        public decimal? amount { get; set; }

        public string? status { get; set; }

        public string? notes { get; set; }

        public DateTime? created_at { get; set; }

        [ForeignKey("purchase_order_id")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("supplier_id")]
        public Supplier? Supplier { get; set; }

        [NotMapped]
        public string? supplier_name { get; set; }

        [NotMapped]
        public string? po_number { get; set; }

        public decimal? po_total { get; set; }

        [NotMapped]
        public string? transaction_name { get; set; }

        [NotMapped]
        public string? transaction_detail { get; set; }
        
    }
}