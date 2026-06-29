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

        Task<(bool success, string message)> ApprovePurchaseOrder(int id);

        Task<bool> UnapprovePurchaseOrder(int id);
    }

    public class PurchaseOrderUsecase : IPurchaseOrderUsecase
    {
        private readonly IPurchaseOrderRepo       _purchaseOrderRepo;
        private readonly ISupplierRepo            _supplierRepo;
        private readonly IPurchaseOrderDetailRepo _purchaseOrderDetailRepo;
        private readonly ISupplierProductRepo     _supplierProductRepo;

        public PurchaseOrderUsecase(
            IPurchaseOrderRepo       purchaseOrderRepo,
            ISupplierRepo            supplierRepo,
            IPurchaseOrderDetailRepo purchaseOrderDetailRepo,
            ISupplierProductRepo     supplierProductRepo
        )
        {
            _purchaseOrderRepo       = purchaseOrderRepo;
            _supplierRepo            = supplierRepo;
            _purchaseOrderDetailRepo = purchaseOrderDetailRepo;
            _supplierProductRepo     = supplierProductRepo;
        }

        public async Task<string> GetNextPONumber()
        {
            return await _purchaseOrderRepo.GeneratePONumber();
        }

        public async Task<int> InsertPurchaseOrder(PurchaseOrder model)
        {
            var supplier =
                await _supplierRepo.GetSupplierById(model.supplier_id);

            if (supplier == null)
                return 0;

            if (supplier.status != "Active")
                return 0;

            model.created_at = DateTime.Now;
            model.status     = "Draft";
            model.po_number  = await _purchaseOrderRepo.GeneratePONumber();

            return await _purchaseOrderRepo.InsertPurchaseOrder(model);
        }

        public async Task<List<PurchaseOrder>> GetAllPurchaseOrder()
        {
            return await _purchaseOrderRepo.GetAllPurchaseOrder();
        }

        public async Task<bool> UpdatePurchaseOrder(PurchaseOrder model)
        {
            var existing =
                await _purchaseOrderRepo.GetPurchaseOrderById(model.purchase_order_id);

            if (existing != null && existing.status == "Completed")
                return false;

            return await _purchaseOrderRepo.UpdatePurchaseOrder(model);
        }

        public async Task<bool> DeletePurchaseOrder(int id)
        {
            var existing =
                await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (existing != null && existing.status == "Approved")
                return false;

            return await _purchaseOrderRepo.DeletePurchaseOrder(id);
        }

        // ─── APPROVE ──────────────────────────────────────────────────────
        // Transitions Draft → Approved and hard-reserves supplier stock for
        // every detail line. Rolls back all deductions if any line fails.

        public async Task<(bool success, string message)> ApprovePurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status == "Approved")
                return (false, "Purchase Order is already approved");

            if (po.status == "Completed")
                return (false, "Purchase Order is already completed");

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            if (details.Count == 0)
                return (false, "Purchase Order has no detail lines");

            // Hard-reserve stock for each line. Track what was deducted so we
            // can roll back if a later line has insufficient stock.
            var deducted = new List<(int productId, int quantity)>();

            foreach (var line in details)
            {
                bool ok = await _supplierProductRepo
                    .DeductStock(line.product_id, po.supplier_id, line.quantity);

                if (!ok)
                {
                    // Restore every deduction made so far
                    foreach (var (pid, qty) in deducted)
                        await _supplierProductRepo
                            .RestoreStock(pid, po.supplier_id, qty);

                    return (false,
                        $"Insufficient stock for product_id {line.product_id}. Approval cancelled.");
                }

                deducted.Add((line.product_id, line.quantity));
            }

            // All lines deducted successfully — flip status to Approved
            po.status = "Approved";
            await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return (true, "Purchase Order approved and stock reserved");
        }

        // ─── UNAPPROVE (undo reservation) ────────────────────────────────
        // Reverses the hard reserve and returns status to Draft.

        public async Task<bool> UnapprovePurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null || po.status != "Approved")
                return false;

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            foreach (var line in details)
                await _supplierProductRepo
                    .RestoreStock(line.product_id, po.supplier_id, line.quantity);

            po.status = "Draft";
            return await _purchaseOrderRepo.UpdatePurchaseOrder(po);
        }
    }
}
