using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseReturnUsecase
    {
        Task<string> GetNextReturnNumber();

        Task<int> InsertPurchaseReturn(PurchaseReturn model);

        Task<List<PurchaseReturn>> GetAllPurchaseReturn();

        Task<bool> UpdatePurchaseReturn(int id, string status, string notes);

        Task<bool> DeletePurchaseReturn(int id);
    }

    public class PurchaseReturnUsecase : IPurchaseReturnUsecase
    {
        private readonly IPurchaseReturnRepo      _purchaseReturnRepo;
        private readonly IGoodsReceiptRepo        _goodsReceiptRepo;
        private readonly IGoodsReceiptDetailRepo  _goodsReceiptDetailRepo;
        private readonly ISupplierProductRepo     _supplierProductRepo;

        public PurchaseReturnUsecase(
            IPurchaseReturnRepo     purchaseReturnRepo,
            IGoodsReceiptRepo       goodsReceiptRepo,
            IGoodsReceiptDetailRepo goodsReceiptDetailRepo,
            ISupplierProductRepo    supplierProductRepo
        )
        {
            _purchaseReturnRepo     = purchaseReturnRepo;
            _goodsReceiptRepo       = goodsReceiptRepo;
            _goodsReceiptDetailRepo = goodsReceiptDetailRepo;
            _supplierProductRepo    = supplierProductRepo;
        }

        public async Task<string> GetNextReturnNumber()
        {
            return await _purchaseReturnRepo.GenerateReturnNumber();
        }

        // When a Purchase Return is created the goods are going back to the
        // supplier, so supplier_products.available_stock is restored for every
        // product line that was on the originating Goods Receipt.
        public async Task<int> InsertPurchaseReturn(PurchaseReturn model)
        {
            model.purchase_return_number =
                await _purchaseReturnRepo.GenerateReturnNumber();

            int returnId = await _purchaseReturnRepo.InsertPurchaseReturn(model);

            if (returnId > 0)
            {
                // Fetch the GR to get the supplier_id (via PO join)
                var gr = await _goodsReceiptRepo
                    .GetGoodsReceiptById(model.goods_receipt_id);

                if (gr != null)
                {
                    var details = await _goodsReceiptDetailRepo
                        .GetDetailsByGoodsReceiptId(model.goods_receipt_id);

                    foreach (var line in details)
                    {
                        await _supplierProductRepo
                            .RestoreStock(line.product_id, gr.supplier_id, line.quantity);
                    }
                }
            }

            return returnId;
        }

        public async Task<List<PurchaseReturn>> GetAllPurchaseReturn()
        {
            return await _purchaseReturnRepo.GetAllPurchaseReturn();
        }

        public async Task<bool> UpdatePurchaseReturn(int id, string status, string notes)
        {
            return await _purchaseReturnRepo.UpdatePurchaseReturn(id, status, notes);
        }

        public async Task<bool> DeletePurchaseReturn(int id)
        {
            return await _purchaseReturnRepo.DeletePurchaseReturn(id);
        }
    }
}
