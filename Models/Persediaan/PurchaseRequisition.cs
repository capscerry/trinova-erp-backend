using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("purchase_requisition")]
    public class PurchaseRequisition
    {
        [Key]
        public int pr_id { get; set; }

        public string pr_number { get; set; } = string.Empty;

        [Required]
        public DateTime pr_date { get; set; }

        [Required]
        public int warehouse_id { get; set; }

        [Required]
        public string status { get; set; } = "REQUESTED";

        public string? remarks { get; set; }

        public DateTime created_at { get; set; }

        public DateTime? updated_at { get; set; }

        [ForeignKey(nameof(warehouse_id))]
        public MasterWarehouse? Warehouse { get; set; }

        public ICollection<PurchaseRequisitionDetail> Details { get; set; }
            = new List<PurchaseRequisitionDetail>();
    }
}