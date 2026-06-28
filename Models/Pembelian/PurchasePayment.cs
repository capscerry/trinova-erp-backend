using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trinova_erp_backend.Models
{
    public class PurchasePayment
    {
        public int purchase_payment_id { get; set; }

        public string payment_number { get; set; } = "";

        public int purchase_invoice_id { get; set; }

        public DateTime payment_date { get; set; }

        public decimal amount { get; set; }

        public string? payment_method { get; set; }

        public string? status { get; set; }

        public string? notes { get; set; }

        public DateTime? created_at { get; set; }

        public string? invoice_number { get; set; }

        public string? supplier_name { get; set; }
    }
}