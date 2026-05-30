using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("stock_transaction")]
    public class StockTransaction
    {
        [Key]
        public int transaction_id { get; set; }

        [Required]
        public int product_id { get; set; }

        [Required]
        public int warehouse_id { get; set; }

        [Required]
        public string transaction_type { get; set; } = string.Empty;

        [Required]
        public decimal quantity { get; set; }

        public string? reference_no { get; set; }

        public string? reference_module { get; set; }

        public int? reference_id { get; set; }

        public string? remarks { get; set; }

        public DateTime created_at { get; set; }

        [ForeignKey(nameof(product_id))]
        public MasterProduct? Product { get; set; }

        [ForeignKey(nameof(warehouse_id))]
        public MasterWarehouse? Warehouse { get; set; }
    }
}