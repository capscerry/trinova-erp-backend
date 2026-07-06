using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("supplier_category")]
    public class SupplierCategory
    {
        [Key]
        public int category_id { get; set; }

        public string category_code { get; set; } = string.Empty;

        public string category_name { get; set; } = string.Empty;

        public DateTime? created_date { get; set; }

        public string? created_by { get; set; }

        public DateTime? update_date { get; set; }

        public string? update_by { get; set; }

        public bool is_active { get; set; } = true;
    }
}