namespace trinova_erp_backend.Models.Penjualan
{
    public class PengirimanPenjualan
    {
        public DeliveryOrderHeaderDTO Header { get; set; }
        public List<DeliveryOrderDetailDTO>  Detail { get; set; }
    }

    public class DeliveryOrderHeaderDTO {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime DoDate { get; set; }
        public string? CustomerName { get; set; }
        public string? DoNumber { get; set; }
        public string? SoNumber { get; set; }

        public int DeliveryCategoryId { get; set; }
        public string? DeliveryShippingName { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
    }

    public class DeliveryOrderDetailDTO
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int UomId { get; set; }
        public string? UomName { get; set; }
        public int QtyDikirim { get; set; }
        public int QtyDipesan { get; set; }
    }


}
