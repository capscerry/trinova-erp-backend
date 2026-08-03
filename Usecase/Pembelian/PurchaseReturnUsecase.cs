using System.Text.Json;
using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public class UnpaidInvoicesResult
    {
        public List<PurchaseInvoice> Invoices { get; init; } = [];
        public bool CashRefundAvailable => Invoices.Count > 0;
    }

    internal sealed class ReturnLineItemDto
    {
        public int    product_id  { get; set; }
        public int    qty_return  { get; set; }
    }

    public interface IPurchaseReturnUsecase
    {
        Task<string> GetNextReturnNumber();

        Task<int> InsertPurchaseReturn(PurchaseReturn model);

        Task<List<PurchaseReturn>> GetAllPurchaseReturn();

        Task<UnpaidInvoicesResult> GetUnpaidInvoicesForReturn(int purchaseReturnId);

        Task<List<GoodsReceiptDetail>> GetReturnDetails(int purchaseReturnId);

        /// <summary>
        /// Returns GR detail lines (with product_name, quantity, remaining_qty)
        /// for only lines where remaining_qty &gt; 0.
        /// Used by the Purchase Return creation modal.
        /// </summary>
        Task<List<GoodsReceiptDetail>> GetAvailableReturnDetails(int goodsReceiptId);

        Task<bool> UpdatePurchaseReturn(
            int id,
            string status,
            string notes,
            string closingCondition,
            int? targetInvoiceId = null
        );

        Task<bool> DeletePurchaseReturn(int id);
    }

    public class PurchaseReturnUsecase : IPurchaseReturnUsecase
    {
        private readonly IPurchaseReturnRepo      _purchaseReturnRepo;
        private readonly IGoodsReceiptRepo        _goodsReceiptRepo;
        private readonly IGoodsReceiptDetailRepo  _goodsReceiptDetailRepo;
        private readonly ISupplierProductRepo     _supplierProductRepo;
        private readonly IPurchaseInvoiceRepo     _purchaseInvoiceRepo;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
        };

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

        /// <summary>
        /// Parses the JSON array stored in <c>transaction_detail</c> into a list
        /// of (product_id, qty_return) pairs.  Returns an empty list when the
        /// field is missing, empty, or not valid JSON — callers fall back to the
        /// full-GR-line path in that case.
        /// </summary>
        private static List<ReturnLineItemDto> ParseReturnItems(string? transactionDetail)
        {
            if (string.IsNullOrWhiteSpace(transactionDetail))
                return [];

            try
            {
                var items = JsonSerializer.Deserialize<List<ReturnLineItemDto>>(
                    transactionDetail, _jsonOpts
                );
                // Filter out rows with zero or negative qty so we never deduct/restore 0
                return items?.Where(i => i.qty_return > 0).ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        public async Task<int> InsertPurchaseReturn(PurchaseReturn model)
        {
            // Duplicate-submission guard: reject if an open return already exists
            // for this Goods Receipt (same GR submitted twice by rapid clicking).
            bool activeReturnExists =
                await _purchaseReturnRepo
                    .IsActiveReturnExist(model.goods_receipt_id);

            if (activeReturnExists)
            {
                throw new InvalidOperationException(
                    "An active Purchase Return already exists for this Goods Receipt."
                );
            }

            if (!string.IsNullOrWhiteSpace(model.settlement_option) &&
                model.settlement_option.Equals("Cash Refund", StringComparison.OrdinalIgnoreCase))
            {
                var gr = await _goodsReceiptRepo.GetGoodsReceiptById(model.goods_receipt_id)
                    ?? throw new Exception("Goods receipt tidak ditemukan.");

                var invoices = await _purchaseInvoiceRepo
                    .GetUnpaidInvoicesBySupplier(gr.supplier_id);

                if (invoices.Count == 0)
                    throw new Exception(
                        "Cash Refund tidak dapat dipilih: tidak ada invoice yang belum lunas " +
                        "untuk supplier ini. Pilih Opsi A (Penggantian Barang) atau " +
                        "Opsi B (Terima Kerugian) sebagai gantinya."
                    );
            }

            // ── Remaining-qty validation ────────────────────────────────
            // Parse the line items that will be returned so we can validate
            // each one against the current remaining_qty in the GR detail.
            var returnItems = ParseReturnItems(model.transaction_detail);

            if (returnItems.Count > 0)
            {
                var lines = returnItems
                    .Select(i => (i.product_id, i.qty_return));

                string? validationError = await _goodsReceiptDetailRepo
                    .ValidateRemainingQty(model.goods_receipt_id, lines);

                if (validationError != null)
                    throw new Exception(validationError);
            }
            else
            {
                // Legacy / no item-level JSON: validate every GR line in full
                var grDetails = await _goodsReceiptDetailRepo
                    .GetDetailsByGoodsReceiptId(model.goods_receipt_id);

                foreach (var line in grDetails)
                {
                    if (line.remaining_qty <= 0)
                        throw new Exception(
                            $"The requested return quantity exceeds the remaining quantity " +
                            $"available in this Goods Receipt (product ID {line.product_id} " +
                            $"has 0 remaining)."
                        );
                }
            }
            // ── End validation ──────────────────────────────────────────

            model.purchase_return_number =
                await _purchaseReturnRepo.GenerateReturnNumber();

            int returnId = await _purchaseReturnRepo.InsertPurchaseReturn(model);

            if (returnId > 0)
            {
                if (returnItems.Count > 0)
                {
                    // Precise path: deduct only the actually-returned quantities
                    foreach (var item in returnItems)
                    {
                        await _goodsReceiptDetailRepo
                            .DeductInventoryStock(item.product_id, item.qty_return);
                    }
                }
                else
                {
                    // Legacy fallback: deduct every GR line
                    var grDetails = await _goodsReceiptDetailRepo
                        .GetDetailsByGoodsReceiptId(model.goods_receipt_id);

                    foreach (var line in grDetails)
                    {
                        await _goodsReceiptDetailRepo
                            .DeductInventoryStock(line.product_id, line.quantity);
                    }
                }
            }

            return returnId;
        }

        public async Task<List<PurchaseReturn>> GetAllPurchaseReturn()
        {
            return await _purchaseReturnRepo.GetAllPurchaseReturn();
        }

        // Walks purchase_return -> goods_receipt -> purchase_order -> supplier_id
        // and returns all unpaid invoices for that supplier with live outstanding amounts,
        // plus a CashRefundAvailable flag the frontend uses to lock/unlock Option C.
        public async Task<UnpaidInvoicesResult> GetUnpaidInvoicesForReturn(
            int purchaseReturnId
        )
        {
            var allReturns = await _purchaseReturnRepo.GetAllPurchaseReturn();
            var pr = allReturns.FirstOrDefault(r => r.purchase_return_id == purchaseReturnId)
                ?? throw new Exception("Purchase return record not found.");

            var gr = await _goodsReceiptRepo.GetGoodsReceiptById(pr.goods_receipt_id)
                ?? throw new Exception("Goods receipt linked to this return was not found.");

            var invoices = await _purchaseInvoiceRepo
                .GetUnpaidInvoicesBySupplier(gr.supplier_id);

            return new UnpaidInvoicesResult { Invoices = invoices };
        }

        public async Task<List<GoodsReceiptDetail>> GetReturnDetails(int purchaseReturnId)
        {
            var allReturns = await _purchaseReturnRepo.GetAllPurchaseReturn();
            var pr = allReturns.FirstOrDefault(r => r.purchase_return_id == purchaseReturnId)
                ?? throw new Exception("Purchase return record not found.");

            return await _goodsReceiptDetailRepo
                .GetDetailsByGoodsReceiptIdWithProductName(pr.goods_receipt_id);
        }

        /// <summary>
        /// Returns only GR detail lines where remaining_qty &gt; 0 for the
        /// given goods_receipt_id. Used by the Purchase Return creation form
        /// so exhausted lines never appear in the product selection UI.
        /// </summary>
        public async Task<List<GoodsReceiptDetail>> GetAvailableReturnDetails(int goodsReceiptId)
        {
            return await _goodsReceiptDetailRepo
                .GetAvailableDetailsByGoodsReceiptId(goodsReceiptId);
        }

        /// <summary>
        /// Reduces remaining_qty for every returned line in a purchase return.
        /// Uses the item-level JSON when available; falls back to full GR lines.
        /// Throws if any line cannot be reduced (race-condition guard).
        /// </summary>
        private async Task ReduceRemainingQtyForReturn(PurchaseReturn pr)
        {
            var returnItems = ParseReturnItems(pr.transaction_detail);

            if (returnItems.Count > 0)
            {
                foreach (var item in returnItems)
                {
                    bool ok = await _goodsReceiptDetailRepo
                        .ReduceRemainingQty(pr.goods_receipt_id, item.product_id, item.qty_return);

                    if (!ok)
                        throw new Exception(
                            $"The requested return quantity for product ID {item.product_id} " +
                            "exceeds the remaining quantity available in this Goods Receipt. " +
                            "Another user may have already processed a return for this item."
                        );
                }
            }
            else
            {
                // Legacy path: no item-level JSON, reduce every GR line by its full quantity
                var grDetails = await _goodsReceiptDetailRepo
                    .GetDetailsByGoodsReceiptId(pr.goods_receipt_id);

                foreach (var line in grDetails)
                {
                    bool ok = await _goodsReceiptDetailRepo
                        .ReduceRemainingQty(pr.goods_receipt_id, line.product_id, line.quantity);

                    if (!ok)
                        throw new Exception(
                            $"The requested return quantity for product ID {line.product_id} " +
                            "exceeds the remaining quantity available in this Goods Receipt."
                        );
                }
            }
        }

        public async Task<bool> UpdatePurchaseReturn(
            int id,
            string status,
            string notes,
            string closingCondition,
            int? targetInvoiceId = null
        )
        {
            var allReturns = await _purchaseReturnRepo.GetAllPurchaseReturn();
            var pr = allReturns.FirstOrDefault(r => r.purchase_return_id == id)
                ?? throw new Exception("Purchase return record not found.");

            if (status == "Closed" &&
                pr.settlement_option.Equals("Cash Refund", StringComparison.OrdinalIgnoreCase))
            {

                var gr = await _goodsReceiptRepo.GetGoodsReceiptById(pr.goods_receipt_id)
                    ?? throw new Exception("Goods receipt linked to this return was not found.");

                int supplierId = gr.supplier_id;

                var candidates =
                    await _purchaseInvoiceRepo.GetUnpaidInvoicesBySupplier(supplierId);

                if (candidates.Count == 0)
                    throw new Exception(
                        "Cash Refund tidak dapat diproses: tidak ada invoice yang belum lunas " +
                        "untuk supplier ini. Pilih Opsi A (Penggantian Barang) atau " +
                        "Opsi B (Terima Kerugian) sebagai gantinya."
                    );

                if (!targetInvoiceId.HasValue)
                    throw new Exception(
                        "Cash Refund membutuhkan pemilihan invoice. " +
                        "Pilih invoice yang ingin dikreditkan sebelum mengkonfirmasi."
                    );

                var targetInvoice = candidates
                    .FirstOrDefault(i => i.purchase_invoice_id == targetInvoiceId.Value);

                if (targetInvoice == null)
                    throw new Exception(
                        "Invoice yang dipilih tidak ditemukan, bukan milik supplier ini, " +
                        "atau sudah lunas sepenuhnya."
                    );

                string condFe =
                    $"Cash refund confirmed. " +
                    $"Rp {pr.total_amount:N0} credited against invoice {targetInvoice.invoice_number}.";

                string notesFe =
                    string.IsNullOrWhiteSpace(notes) ? condFe : notes;

                bool updated = await _purchaseReturnRepo.UpdatePurchaseReturn(
                    id, status, notesFe, condFe
                );

                if (updated)
                {
                    await ReduceRemainingQtyForReturn(pr);
                    await _goodsReceiptRepo.UpdateGoodsReceiptStatus(
                        pr.goods_receipt_id, "Returned"
                    );
                }

                return updated;
            }

            if (status == "Closed" &&
                pr.settlement_option.Equals("Accept Loss", StringComparison.OrdinalIgnoreCase))
            {
                var returnItems = ParseReturnItems(pr.transaction_detail);

                int restoredLineCount;

                if (returnItems.Count > 0)
                {
                    foreach (var item in returnItems)
                    {
                        await _goodsReceiptDetailRepo
                            .RestoreInventoryStock(item.product_id, item.qty_return);
                    }
                    restoredLineCount = returnItems.Count;
                }
                else
                {
                    var grDetails = await _goodsReceiptDetailRepo
                        .GetDetailsByGoodsReceiptId(pr.goods_receipt_id);

                    foreach (var line in grDetails)
                    {
                        await _goodsReceiptDetailRepo
                            .RestoreInventoryStock(line.product_id, line.quantity);
                    }
                    restoredLineCount = grDetails.Count;
                }

                string generatedCondition =
                    $"Accept Loss settled. Supplier returned fixed goods worth Rp {pr.total_amount:N0}. " +
                    $"Stock restored for {restoredLineCount} product line(s).";

                string generatedNotes =
                    string.IsNullOrWhiteSpace(notes)
                        ? generatedCondition
                        : notes;

                bool updated = await _purchaseReturnRepo.UpdatePurchaseReturn(
                    id, status, generatedNotes, generatedCondition
                );

                if (updated)
                {
                    await ReduceRemainingQtyForReturn(pr);
                    await _goodsReceiptRepo.UpdateGoodsReceiptStatus(
                        pr.goods_receipt_id, "Returned"
                    );
                }

                return updated;
            }

            bool returnUpdated = await _purchaseReturnRepo.UpdatePurchaseReturn(
                id, status, notes, closingCondition
            );

            if (returnUpdated && status == "Closed")
            {
                await ReduceRemainingQtyForReturn(pr);
                await _goodsReceiptRepo.UpdateGoodsReceiptStatus(
                    pr.goods_receipt_id, "Returned"
                );
            }

            return returnUpdated;
        }

        public async Task<bool> DeletePurchaseReturn(int id)
        {
            return await _purchaseReturnRepo.DeletePurchaseReturn(id);
        }
    }
}
