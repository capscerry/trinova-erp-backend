using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("master_warehouse")]
    public class MasterWarehouse
    {
        [Key]
        public int warehouse_id { get; set; }

        [Required]
        public string? warehouse_name { get; set; }

        public string? description { get; set; }

        [Required]
        public string? warehouse_address { get; set; }


        [Required]
        public string? warehouse_type { get; set; }

        public DateTime? created_at { get; set; }

        public string? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public string? updated_by { get; set; }

        public bool? is_active { get; set; }
    }
}