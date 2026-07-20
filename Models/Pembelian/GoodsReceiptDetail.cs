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

        /// <summary>
        /// Original received quantity. Never modified after creation.
        /// </summary>
        public int quantity { get; set; }

        /// <summary>
        /// Units still available for a Purchase Return.
        /// Decremented each time a Purchase Return settlement is completed.
        /// Starts equal to <see cref="quantity"/> and never goes below 0.
        /// </summary>
        public int remaining_qty { get; set; }

        /// <summary>
        /// Not stored in the table — populated by joined queries so the
        /// frontend can display product names (e.g. in the settlement modal).
        /// </summary>
        [NotMapped]
        public string? product_name { get; set; }

        [ForeignKey("goods_receipt_id")]
        public GoodsReceipt? GoodsReceipt { get; set; }
    }
}