using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    /// <summary>
    /// Payload returned by <see cref="IPurchaseReturnUsecase.GetUnpaidInvoicesForReturn"/>.
    /// </summary>
    public class UnpaidInvoicesResult
    {
        /// <summary>
        /// All unpaid / partially-paid invoices for the supplier linked to this return.
        /// Empty when none exist.
        /// </summary>
        public List<PurchaseInvoice> Invoices { get; init; } = [];

        /// <summary>
        /// True when at least one outstanding invoice exists, meaning Option C
        /// (Cash Refund) is available to the user. False locks Option C on the frontend.
        /// </summary>
        public bool CashRefundAvailable => Invoices.Count > 0;
    }

    public interface IPurchaseReturnUsecase
    {
        Task<string> GetNextReturnNumber();

        Task<int> InsertPurchaseReturn(PurchaseReturn model);

        Task<List<PurchaseReturn>> GetAllPurchaseReturn();

        /// <summary>
        /// Resolves the supplier for the given purchase return by walking
        /// purchase_return -> goods_receipt -> purchase_order -> supplier_id,
        /// then returns all unpaid/partially-paid invoices for that supplier
        /// with real-time outstanding amounts, plus a <c>CashRefundAvailable</c>
        /// flag the frontend uses to lock/unlock Option C.
        /// The frontend only needs the purchase_return_id — no supplier_id required.
        /// </summary>
        Task<UnpaidInvoicesResult> GetUnpaidInvoicesForReturn(int purchaseReturnId);

        /// <summary>
        /// Returns the product lines (name + quantity) that belong to the
        /// Goods Receipt linked to this purchase return.
        /// Used by the Accept Loss modal so the frontend always shows the
        /// correct products and quantities for the selected return.
        /// </summary>
        Task<List<GoodsReceiptDetail>> GetReturnDetails(int purchaseReturnId);

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
        // was on the originating Goods Receipt.
        public async Task<int> InsertPurchaseReturn(PurchaseReturn model)
        {
            // Guard: Cash Refund requires at least one outstanding invoice from
            // this supplier. Reject at creation time so the record is never
            // persisted with a settlement option that cannot be fulfilled.
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

        // Returns the GR detail lines with product names for the Accept Loss modal.
        // Resolves: purchase_return_id -> goods_receipt_id -> goods_receipt_detail + master_product
        public async Task<List<GoodsReceiptDetail>> GetReturnDetails(int purchaseReturnId)
        {
            var allReturns = await _purchaseReturnRepo.GetAllPurchaseReturn();
            var pr = allReturns.FirstOrDefault(r => r.purchase_return_id == purchaseReturnId)
                ?? throw new Exception("Purchase return record not found.");

            return await _goodsReceiptDetailRepo
                .GetDetailsByGoodsReceiptIdWithProductName(pr.goods_receipt_id);
        }

        public async Task<bool> UpdatePurchaseReturn(
            int id,
            string status,
            string notes,
            string closingCondition,
            int? targetInvoiceId = null
        )
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

                // Early guard: Cash Refund is only valid when the supplier has at
                // least one outstanding invoice. If none exist, block immediately so
                // the message is consistent whether the call came from the UI or the API.
                var candidates =
                    await _purchaseInvoiceRepo.GetUnpaidInvoicesBySupplier(supplierId);

                if (candidates.Count == 0)
                    throw new Exception(
                        "Cash Refund tidak dapat diproses: tidak ada invoice yang belum lunas " +
                        "untuk supplier ini. Pilih Opsi A (Penggantian Barang) atau " +
                        "Opsi B (Terima Kerugian) sebagai gantinya."
                    );

                // An invoice must be explicitly chosen — we never auto-pick one.
                if (!targetInvoiceId.HasValue)
                    throw new Exception(
                        "Cash Refund membutuhkan pemilihan invoice. " +
                        "Pilih invoice yang ingin dikreditkan sebelum mengkonfirmasi."
                    );

                // Validate the chosen invoice still belongs to this supplier and is still open.
                // The frontend already inserted the Return Credit payment row before calling
                // this endpoint, so we only audit — do NOT call ApplyCreditToInvoice again.
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

                return await _purchaseReturnRepo.UpdatePurchaseReturn(
                    id, status, notesFe, condFe
                );
            }

            // Accept Loss: restore stock for every product line on the originating GR.
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
                    id, status, generatedNotes, generatedCondition
                );
            }

            // All other status / settlement combinations — plain update
            return await _purchaseReturnRepo.UpdatePurchaseReturn(
                id, status, notes, closingCondition
            );
        }

        public async Task<bool> DeletePurchaseReturn(int id)
        {
            return await _purchaseReturnRepo.DeletePurchaseReturn(id);
        }
    }
}
