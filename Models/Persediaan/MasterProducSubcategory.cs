using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("master_product_subcategory")]
    public class ProductSubcategory
    {
        [Key]
        public int subcategory_id { get; set; }

        [Required]
        public int category_id { get; set; }

        [ForeignKey(nameof(category_id))]
        public MasterProductCategory? Category { get; set; }

        [Required]
        [Column("subcategory_code")]
        [StringLength(2)]
        public string code { get; set; } = string.Empty;

        [Required]
        [Column("subcategory_name")]
        [StringLength(100)]
        public string name { get; set; } = string.Empty;

        public bool is_active { get; set; }

        public DateTime created_at { get; set; }

        public DateTime updated_at { get; set; }
    }
}