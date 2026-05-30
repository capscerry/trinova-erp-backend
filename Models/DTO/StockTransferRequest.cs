namespace trinova_erp_backend.Models.Persediaan.DTO
{
    public class StockTransferRequest
    {
        public int product_id { get; set; }

        public int source_warehouse_id { get; set; }

        public int destination_warehouse_id { get; set; }

        public decimal quantity { get; set; }

        public string? notes { get; set; }

        public string? created_by { get; set; }
    }
}