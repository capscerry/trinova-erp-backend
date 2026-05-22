using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models.Persediaan
{
    [Table("master_product")]
    public class MasterProduct
    {
        [Key]
        public int product_id { get; set; }

        public string? product_name { get; set; }

        public string? product_code { get; set; }

        public string? product_type { get; set; }

        public int uom_id { get; set; }

        public int category_id { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        [ForeignKey("uom_id")]
        public MasterUom? MasterUom { get; set; }

        [ForeignKey("category_id")]
        public MasterProductCategory? MasterProductCategory { get; set; }
    }
}