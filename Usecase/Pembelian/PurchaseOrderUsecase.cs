using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseOrderUsecase
    {
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

        public async Task<int> InsertPurchaseOrder(PurchaseOrder model)
        {
            // VALIDATE SUPPLIER
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

            // AUTO CREATED DATE
            model.created_at = DateTime.Now;

            // DEFAULT STATUS
            model.status = "Draft";

            // AUTO GENERATE PO NUMBER
            string today = DateTime.Now.ToString("yyyyMMdd");

            model.po_number =
                $"PO-{today}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

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