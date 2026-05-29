using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("purchase_requisition_detail")]
    public class PurchaseRequisitionDetail
    {
        [Key]
        public int pr_detail_id { get; set; }

        [Required]
        public int pr_id { get; set; }

        [Required]
        public int product_id { get; set; }

        [Required]
        public decimal qty_requested { get; set; }

        public decimal qty_processed { get; set; } = 0;

        public string? remarks { get; set; }

        [ForeignKey(nameof(pr_id))]
        public PurchaseRequisition? PurchaseRequisition { get; set; }

        [ForeignKey(nameof(product_id))]
        public MasterProduct? Product { get; set; }
    }
}