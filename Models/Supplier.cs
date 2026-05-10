using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("master_supplier")]
    public class Supplier
    {
        [Key]
        public int supplier_id { get; set; }

        public string supplier_code { get; set; } = string.Empty;

        public string supplier_name { get; set; } = string.Empty;

        public int? category_supplier { get; set; }

        public string? no_telp_bisnis { get; set; }

        public string? no_telp_wa { get; set; }

        public string? alamat { get; set; }

        public string? email { get; set; }

        public string? faximili { get; set; }

        public string? website { get; set; }

        public DateTime? created_date { get; set; }

        public string? created_by { get; set; }

        public DateTime? update_date { get; set; }

        public string? update_by { get; set; }

        public string? status { get; set; }

        public string? type_supplier { get; set; }

        [ForeignKey("category_supplier")]
        public SupplierCategory? SupplierCategory { get; set; }
    }
}