using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("stock_movement")]
    public class StockMovement
    {
        [Key]
        public int movement_id { get; set; }

        public int product_id { get; set; }

        public string? product_name { get; set; }

        public string? movement_type { get; set; }

        public decimal quantity { get; set; }

        public string? reference_number { get; set; }

        public string? notes { get; set; }

        public DateTime? movement_date { get; set; }

        public string? created_by { get; set; }

        public DateTime? created_at { get; set; }

        public int? source_warehouse_id { get; set; }

        public string? warehouse_name { get; set; }

        public int? destination_warehouse_id { get; set; }
    }
}