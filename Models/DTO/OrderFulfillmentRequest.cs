namespace trinova_erp_backend.Models.Persediaan.DTO
{
    public class OrderFulfillmentRequest
    {
        public int product_id { get; set; }

        public int warehouse_id { get; set; }

        public decimal quantity { get; set; }

        public string? notes { get; set; }

        public string? created_by { get; set; }

        public string? reference_no { get; set; }
    }
}