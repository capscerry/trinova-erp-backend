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

        public decimal payment_paid { get; set; }

        public decimal outstanding_amount { get; set; }

        public string? transaction_name { get; set; }

        public string? transaction_detail { get; set; }

        /// <summary>
        /// Nomor Faktur Pajak — unique, auto-incremented (FP-NNNNNNNNNN).
        /// Auto-populated from the linked Purchase Order when its tax fields
        /// (tax_percentage / tax_amount) are non-zero. Null when the PO has
        /// no tax or when the user intentionally leaves the field blank.
        /// </summary>
        public string? nomor_faktur_pajak { get; set; }

    }
}