using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("master_product_category")]
    public class MasterProductCategory
    {
        [Key]
        public int category_id { get; set; }

        public string? category_name { get; set; }

        public DateTime? created_at { get; set; }

        public string? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public string? updated_by { get; set; }

        public bool? is_active { get; set; }
    }
}