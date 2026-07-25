using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseOrderUsecase
    {
        Task<string> GetNextPONumber();

        Task<int> InsertPurchaseOrder(PurchaseOrder model);

        Task<List<PurchaseOrder>> GetAllPurchaseOrder();

        Task<List<PurchaseOrder>> GetPurchaseOrdersByStatus(string status);

        Task<List<PurchaseOrder>> GetApprovedAndCompletedPurchaseOrders();

        Task<PurchaseOrder?> GetPurchaseOrderById(int id);

        Task<bool> UpdatePurchaseOrder(PurchaseOrder model);

        Task<bool> DeletePurchaseOrder(int id);

        Task<(bool success, string message)> RequestApproval(int id);

        Task<(bool success, string message)> ApprovePurchaseOrder(int id);

        Task<(bool success, string message)> RejectPurchaseOrder(int id);

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

        public async Task<List<PurchaseOrder>> GetPurchaseOrdersByStatus(string status)
        {
            return await _purchaseOrderRepo.GetPurchaseOrdersByStatus(status);
        }

        public async Task<List<PurchaseOrder>> GetApprovedAndCompletedPurchaseOrders()
        {
            return await _purchaseOrderRepo.GetApprovedAndCompletedPurchaseOrders();
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderById(int id)
        {
            return await _purchaseOrderRepo.GetPurchaseOrderById(id);
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

        // ─── REQUEST APPROVAL ─────────────────────────────────────────────
        // Transitions Draft → Waiting for Approval.
        // Allows the requester to submit the PO for manager review.

        public async Task<(bool success, string message)> RequestApproval(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status != "Draft")
                return (false, $"Cannot request approval: Purchase Order is currently '{po.status}'. Only Draft POs can be submitted for approval.");

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            if (details.Count == 0)
                return (false, $"Cannot request approval: Purchase Order (id={id}) has no detail lines.");

            po.status = "Waiting for Approval";
            var updated = await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return updated
                ? (true, "Purchase Order submitted for approval")
                : (false, "Failed to update Purchase Order status");
        }

        public async Task<(bool success, string message)> ApprovePurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status == "Approved")
                return (false, "Purchase Order is already approved");

            if (po.status == "Completed")
                return (false, "Purchase Order is already completed");

            if (po.status != "Draft" && po.status != "Waiting for Approval")
                return (false, $"Cannot approve: Purchase Order is currently '{po.status}'");

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            if (details.Count == 0)
                return (false, $"Purchase Order (id={id}, po_number={po.po_number}) has no detail lines");

            // Hard-reserve stock for each line. Track what was actually deducted
            // so we can roll back if a later line has insufficient stock.
            var deducted = new List<(int productId, int quantity)>();

            foreach (var line in details)
            {
                var result = await _supplierProductRepo
                    .DeductStock(line.product_id, po.supplier_id, line.quantity);

                switch (result)
                {
                    case DeductStockResult.Deducted:
                        deducted.Add((line.product_id, line.quantity));
                        break;

                    case DeductStockResult.RowNotFound:
                        break;

                    case DeductStockResult.InsufficientStock:
                        foreach (var (pid, qty) in deducted)
                            await _supplierProductRepo
                                .RestoreStock(pid, po.supplier_id, qty);

                        return (false,
                            $"Insufficient stock for product_id {line.product_id}. Approval cancelled.");
                }
            }

            // All lines processed — flip status to Approved.
            po.status = "Approved";
            await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return (true, "Purchase Order approved and stock reserved");
        }

        public async Task<(bool success, string message)> RejectPurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status != "Waiting for Approval")
                return (false, $"Cannot reject: Purchase Order is currently '{po.status}'. Only POs awaiting approval can be rejected.");

            po.status = "Draft";
            var updated = await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return updated
                ? (true, "Purchase Order rejected and returned to Draft")
                : (false, "Failed to update Purchase Order status");
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
