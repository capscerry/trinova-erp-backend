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

        public int supplier_id { get; set; }

        public string? supplier_name { get; set; }

        public string? po_number { get; set; }

        public decimal? total_amount { get; set; }

        [NotMapped]
        public string? transaction_name { get; set; }

        [NotMapped]
        public string? transaction_detail { get; set; }

        [NotMapped]
        public string? nomor_faktur_pajak { get; set; }

        [ForeignKey("purchase_order_id")]
        public PurchaseOrder? PurchaseOrder { get; set; }
    }
}