using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseOrderUsecase
    {
        Task<string> GetNextPONumber();

        Task<int> InsertPurchaseOrder(PurchaseOrder model);

        Task<List<PurchaseOrder>> GetAllPurchaseOrder();

        Task<bool> UpdatePurchaseOrder(PurchaseOrder model);

        Task<bool> DeletePurchaseOrder(int id);
    }

    public class PurchaseOrderUsecase : IPurchaseOrderUsecase
    {
        private readonly IPurchaseOrderRepo _purchaseOrderRepo;
        private readonly ISupplierRepo _supplierRepo;

        public PurchaseOrderUsecase(
            IPurchaseOrderRepo purchaseOrderRepo,
            ISupplierRepo supplierRepo
        )
        {
            _purchaseOrderRepo = purchaseOrderRepo;
            _supplierRepo = supplierRepo;
        }

        public async Task<string> GetNextPONumber()
        {
            return await _purchaseOrderRepo.GeneratePONumber();
        }

        public async Task<int> InsertPurchaseOrder(PurchaseOrder model)
        {
            var supplier =
                await _supplierRepo.GetSupplierById(
                    model.supplier_id
                );

            if (supplier == null)
            {
                return 0;
            }

            if (supplier.status != "Active")
            {
                return 0;
            }

            model.created_at = DateTime.Now;

            model.status = "Draft";

            model.po_number =
                await _purchaseOrderRepo.GeneratePONumber();

            var result =
                await _purchaseOrderRepo.InsertPurchaseOrder(model);

            return result;
        }

        public async Task<List<PurchaseOrder>> GetAllPurchaseOrder()
        {
            var result = await _purchaseOrderRepo
                .GetAllPurchaseOrder();

            return result;
        }

        public async Task<bool> UpdatePurchaseOrder(PurchaseOrder model)
        {
            var existingPurchaseOrder =
                await _purchaseOrderRepo.GetPurchaseOrderById(
                    model.purchase_order_id
                );

            if (
                existingPurchaseOrder != null
                && existingPurchaseOrder.status == "Completed"
            )
            {
                return false;
            }

            var result = await _purchaseOrderRepo
                .UpdatePurchaseOrder(model);

            return result;
        }

        public async Task<bool> DeletePurchaseOrder(int id)
        {
            var existingPurchaseOrder =
                await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (
                existingPurchaseOrder != null
                && existingPurchaseOrder.status == "Approved"
            )
            {
                return false;
            }

            var result = await _purchaseOrderRepo
                .DeletePurchaseOrder(id);

            return result;
        }
    }
}