using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("goods_receipt_detail")]
    public class GoodsReceiptDetail
    {
        [Key]
        public int goods_receipt_detail_id { get; set; }

        public int goods_receipt_id { get; set; }

        public int product_id { get; set; }

        public int quantity { get; set; }

        /// <summary>
        /// Not stored in the table — populated by the joined query used for
        /// the Accept Loss modal so the frontend can display product names.
        /// </summary>
        [NotMapped]
        public string? product_name { get; set; }

        [ForeignKey("goods_receipt_id")]
        public GoodsReceipt? GoodsReceipt { get; set; }
    }
}