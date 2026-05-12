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
    }
}