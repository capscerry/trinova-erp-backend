using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("purchase_order")]
    public class PurchaseOrder
    {
        [Key]
        public int purchase_order_id { get; set; }

        public string po_number { get; set; } = string.Empty;

        public int supplier_id { get; set; }

        public DateTime? order_date { get; set; }

        public string? status { get; set; }

        public decimal? tax_percentage { get; set; }

        public decimal? tax_amount { get; set; }

        public decimal? total_amount { get; set; }

        public string? transaction_name { get; set; }

        public string? transaction_detail { get; set; }

        public DateTime? expected_date { get; set; }

        public DateTime? created_at { get; set; }

        [ForeignKey("supplier_id")]
        public Supplier? Supplier { get; set; }
    }
}