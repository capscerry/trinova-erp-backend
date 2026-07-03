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

        public int uom_id { get; set; }

        public int category_id { get; set; }

        public int subcategory_id { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        [ForeignKey(nameof(uom_id))]
        public MasterUom? MasterUom { get; set; }

        [ForeignKey(nameof(category_id))]
        public MasterProductCategory? MasterProductCategory { get; set; }

        [ForeignKey(nameof(subcategory_id))]
        public ProductSubcategory? ProductSubcategory { get; set; }
    }
}