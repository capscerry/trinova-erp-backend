using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("inventory_stock")]
    public class InventoryStock
    {
        [Key]
        public int stock_id { get; set; }

        [Required]
        public int product_id { get; set; }

        [Required]
        public int quantity { get; set; }

        public int? minimum_stock { get; set; }

        public int? maximum_stock { get; set; }

        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }
    }
}