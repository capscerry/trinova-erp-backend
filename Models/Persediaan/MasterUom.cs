using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    [Table("master_uom")]
    public class MasterUom
    {
        [Key]
        public int uom_id { get; set; }

        public string? uom_code { get; set; }

        public string? uom_name { get; set; }

        public DateTime? created_at { get; set; }

        public string? created_by { get; set; }

        public DateTime? updated_at { get; set; }

        public string? updated_by { get; set; }

        public bool? is_active { get; set; }
    }
}