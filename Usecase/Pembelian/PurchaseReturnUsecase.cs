using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseReturnUsecase
    {
        Task<string> GetNextReturnNumber();

        Task<int> InsertPurchaseReturn(PurchaseReturn model);

        Task<List<PurchaseReturn>> GetAllPurchaseReturn();

        Task<bool> UpdatePurchaseReturn(int id, string status, string notes, string closingCondition, int? targetInvoiceId = null);

        Task<bool> DeletePurchaseReturn(int id);
    }

    public class PurchaseReturnUsecase : IPurchaseReturnUsecase
    {
        private readonly IPurchaseReturnRepo      _purchaseReturnRepo;
        private readonly IGoodsReceiptRepo        _goodsReceiptRepo;
        private readonly IGoodsReceiptDetailRepo  _goodsReceiptDetailRepo;
        private readonly ISupplierProductRepo     _supplierProductRepo;
        private readonly IPurchaseInvoiceRepo     _purchaseInvoiceRepo;

        public PurchaseReturnUsecase(
            IPurchaseReturnRepo     purchaseReturnRepo,
            IGoodsReceiptRepo       goodsReceiptRepo,
            IGoodsReceiptDetailRepo goodsReceiptDetailRepo,
            ISupplierProductRepo    supplierProductRepo,
            IPurchaseInvoiceRepo    purchaseInvoiceRepo
        )
        {
            _purchaseReturnRepo     = purchaseReturnRepo;
            _goodsReceiptRepo       = goodsReceiptRepo;
            _goodsReceiptDetailRepo = goodsReceiptDetailRepo;
            _supplierProductRepo    = supplierProductRepo;
            _purchaseInvoiceRepo    = purchaseInvoiceRepo;
        }

        public async Task<string> GetNextReturnNumber()
        {
            return await _purchaseReturnRepo.GenerateReturnNumber();
        }

        // When a Purchase Return is created the goods are going back to the
        // supplier, so inventory_stock is reduced for every product line that
        // was on the originating Goods Receipt, and supplier_products is
        // re-synced accordingly.
        public async Task<int> InsertPurchaseReturn(PurchaseReturn model)
        {
            model.purchase_return_number =
                await _purchaseReturnRepo.GenerateReturnNumber();

            int returnId = await _purchaseReturnRepo.InsertPurchaseReturn(model);

            if (returnId > 0)
            {
                var details = await _goodsReceiptDetailRepo
                    .GetDetailsByGoodsReceiptId(model.goods_receipt_id);

                foreach (var line in details)
                {
                    await _goodsReceiptDetailRepo
                        .DeductInventoryStock(line.product_id, line.quantity);
                }
            }

            return returnId;
        }

        public async Task<List<PurchaseReturn>> GetAllPurchaseReturn()
        {
            return await _purchaseReturnRepo.GetAllPurchaseReturn();
        }

        public async Task<bool> UpdatePurchaseReturn(int id, string status, string notes, string closingCondition, int? targetInvoiceId = null)
        {
            // Fetch the return record so we can read settlement_option + supplier
            var allReturns = await _purchaseReturnRepo.GetAllPurchaseReturn();
            var pr = allReturns.FirstOrDefault(r => r.purchase_return_id == id)
                ?? throw new Exception("Purchase return record not found.");

            // Cash Refund: apply a credit against the supplier's outstanding invoice
            if (status == "Closed" &&
                pr.settlement_option.Equals("Cash Refund", StringComparison.OrdinalIgnoreCase))
            {
                // Resolve the supplier_id via the originating GR
                var gr = await _goodsReceiptRepo.GetGoodsReceiptById(pr.goods_receipt_id)
                    ?? throw new Exception("Goods receipt linked to this return was not found.");

                int supplierId = gr.supplier_id;

                PurchaseInvoice? targetInvoice;

                if (targetInvoiceId.HasValue)
                {
                    // User explicitly selected an invoice in the settlement dialog —
                    // verify it belongs to this supplier and is still unpaid.
                    var candidates =
                        await _purchaseInvoiceRepo.GetUnpaidInvoicesBySupplier(supplierId);

                    targetInvoice = candidates
                        .FirstOrDefault(i => i.purchase_invoice_id == targetInvoiceId.Value);

                    if (targetInvoice == null)
                        throw new Exception(
                            "The selected invoice was not found, does not belong to this supplier, " +
                            "or has already been fully paid."
                        );
                }
                else
                {
                    // No invoice specified — fall back to the oldest eligible invoice
                    // (legacy behaviour preserved for backwards compatibility).
                    targetInvoice =
                        await _purchaseInvoiceRepo.GetUnfinishedInvoiceBySupplier(supplierId);

                    if (targetInvoice == null)
                        throw new Exception(
                            "Cash Refund is only allowed when the supplier has an " +
                            "outstanding (unpaid or partially paid) invoice. " +
                            "No eligible invoice was found for this supplier."
                        );
                }

                // Apply the credit — reduces the invoice's outstanding_amount
                await _purchaseInvoiceRepo.ApplyCreditToInvoice(
                    targetInvoice.purchase_invoice_id,
                    pr.total_amount,
                    targetInvoice.invoice_number
                );

                // Build the audit trail stored on the return record itself
                string generatedCondition =
                    $"Cash refund confirmed. " +
                    $"Rp {pr.total_amount:N0} credited against invoice {targetInvoice.invoice_number}.";

                string generatedNotes =
                    string.IsNullOrWhiteSpace(notes)
                        ? generatedCondition
                        : notes;

                return await _purchaseReturnRepo.UpdatePurchaseReturn(
                    id,
                    status,
                    generatedNotes,
                    generatedCondition
                );
            }

            // Accept Loss: the supplier ships back the exact same goods that were returned.
            // Stock was deducted when the return was created (InsertPurchaseReturn),
            // so on settlement we restore it for every product line on the originating GR.
            if (status == "Closed" &&
                pr.settlement_option.Equals("Accept Loss", StringComparison.OrdinalIgnoreCase))
            {
                var gr = await _goodsReceiptRepo.GetGoodsReceiptById(pr.goods_receipt_id)
                    ?? throw new Exception("Goods receipt linked to this return was not found.");

                var details = await _goodsReceiptDetailRepo
                    .GetDetailsByGoodsReceiptId(pr.goods_receipt_id);

                foreach (var line in details)
                {
                    await _goodsReceiptDetailRepo
                        .RestoreInventoryStock(line.product_id, line.quantity);
                }

                string generatedCondition =
                    $"Accept Loss settled. Supplier returned fixed goods worth Rp {pr.total_amount:N0}. " +
                    $"Stock restored for {details.Count} product line(s).";

                string generatedNotes =
                    string.IsNullOrWhiteSpace(notes)
                        ? generatedCondition
                        : notes;

                return await _purchaseReturnRepo.UpdatePurchaseReturn(
                    id,
                    status,
                    generatedNotes,
                    generatedCondition
                );
            }

            // All other status / settlement combinations — plain update
            return await _purchaseReturnRepo.UpdatePurchaseReturn(
                id,
                status,
                notes,
                closingCondition
            );
        }

        public async Task<bool> DeletePurchaseReturn(int id)
        {
            return await _purchaseReturnRepo.DeletePurchaseReturn(id);
        }
    }
}
