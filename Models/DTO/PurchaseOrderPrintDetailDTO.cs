using trinova_erp_backend.Models;

namespace trinova_erp_backend.Models.DTO
{
    public class PurchaseOrderPrintDetailDTO
    {
        public PurchaseOrder Header { get; set; } = new();
        public Supplier? Supplier { get; set; }
        public List<PurchaseOrderDetailWithProductDTO> Details { get; set; } = new();
    }
}
