using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("inventory_stock")]
    public class InventoryStock
    {
        [Key]
        public int stock_id { get; set; }

        public int product_id { get; set; }

        public int warehouse_id { get; set; }

        public decimal qty_on_hand { get; set; }

        public decimal qty_reserved { get; set; }

        public decimal qty_available { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        [ForeignKey(nameof(product_id))]
        public MasterProduct? Product { get; set; }

        [ForeignKey(nameof(warehouse_id))]
        public MasterWarehouse? Warehouse { get; set; }
    }
}