using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    public class PurchaseInvoice
    {
        public int purchase_invoice_id { get; set; }

        public int goods_receipt_id { get; set; }

        public string invoice_number { get; set; } = "";

        public DateTime invoice_date { get; set; }

        public int supplier_id { get; set; }

        public decimal total_amount { get; set; }

        public string status { get; set; } = "";

        public string? supplier_name { get; set; }

        public DateTime created_at { get; set; }

        public decimal dp_paid { get; set; }

        public decimal outstanding_amount { get; set; }

    }
}