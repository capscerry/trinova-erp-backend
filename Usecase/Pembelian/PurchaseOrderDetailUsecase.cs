using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseOrderDetailUsecase
    {
        Task<string> InsertPurchaseOrderDetail(PurchaseOrderDetail model);

        Task<List<PurchaseOrderDetail>> GetAllPurchaseOrderDetail();

        Task<bool> UpdatePurchaseOrderDetail(PurchaseOrderDetail model);

        Task<bool> DeletePurchaseOrderDetail(int id);
    }

    public class PurchaseOrderDetailUsecase : IPurchaseOrderDetailUsecase
    {
        private readonly IPurchaseOrderDetailRepo _purchaseOrderDetailRepo;
        private readonly InventoryStockRepo       _inventoryStockRepo;

        public PurchaseOrderDetailUsecase(
            IPurchaseOrderDetailRepo purchaseOrderDetailRepo,
            InventoryStockRepo       inventoryStockRepo
        )
        {
            _purchaseOrderDetailRepo = purchaseOrderDetailRepo;
            _inventoryStockRepo      = inventoryStockRepo;
        }

        public async Task<string> InsertPurchaseOrderDetail(
            PurchaseOrderDetail model
        )
        {
            // prevents PO creation based on unconfirmed/phantom stock.
            var hasStock = await _inventoryStockRepo
                .HasConfirmedStockAsync(model.product_id);

            if (!hasStock)
                return "Product has no confirmed inventory stock. " +
                       "Please ensure the product has been received into " +
                       "inventory before adding it to a Purchase Order.";

            model.subtotal = model.quantity * model.price;

            var result = await _purchaseOrderDetailRepo
                .InsertPurchaseOrderDetail(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<PurchaseOrderDetail>>
            GetAllPurchaseOrderDetail()
        {
            var result = await _purchaseOrderDetailRepo
                .GetAllPurchaseOrderDetail();

            return result;
        }

        public async Task<bool> UpdatePurchaseOrderDetail(
            PurchaseOrderDetail model
        )
        {
            // AUTO RECALCULATE SUBTOTAL
            model.subtotal = model.quantity * model.price;

            var result = await _purchaseOrderDetailRepo
                .UpdatePurchaseOrderDetail(model);

            return result;
        }

        public async Task<bool> DeletePurchaseOrderDetail(int id)
        {
            var result = await _purchaseOrderDetailRepo
                .DeletePurchaseOrderDetail(id);

            return result;
        }
    }
}