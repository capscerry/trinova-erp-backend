using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IGoodsReceiptUsecase
    {
        Task<int> InsertGoodsReceipt(GoodsReceipt model);

        Task<List<GoodsReceipt>> GetAllGoodsReceipt();

        Task<bool> UpdateGoodsReceipt(GoodsReceipt model);

        Task<bool> DeleteGoodsReceipt(int id);
    }

    public class GoodsReceiptUsecase : IGoodsReceiptUsecase
    {
        private readonly IGoodsReceiptRepo _goodsReceiptRepo;
        private readonly IPurchaseOrderRepo _purchaseOrderRepo;

        public GoodsReceiptUsecase(
            IGoodsReceiptRepo goodsReceiptRepo,
            IPurchaseOrderRepo purchaseOrderRepo
        )
        {
            _goodsReceiptRepo = goodsReceiptRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
        }

        public async Task<int> InsertGoodsReceipt(GoodsReceipt model)
        {
            // AUTO CREATED DATE
            model.created_at = DateTime.Now;

            // DEFAULT STATUS
            model.status = "Received";

            // AUTO GENERATE RECEIPT NUMBER
            string today = DateTime.Now.ToString("yyyyMMdd");

            model.receipt_number =
                $"GR-{today}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

            // AUTO COMPLETE PURCHASE ORDER
            var purchaseOrder =
                await _purchaseOrderRepo.GetPurchaseOrderById(
                    model.purchase_order_id
                );

            if (purchaseOrder != null)
            {
                purchaseOrder.status = "Completed";

                await _purchaseOrderRepo
                    .UpdatePurchaseOrder(purchaseOrder);
            }

            int goodsReceiptId =
                await _goodsReceiptRepo.InsertGoodsReceipt(model);

            return goodsReceiptId;
        }

        public async Task<List<GoodsReceipt>> GetAllGoodsReceipt()
        {
            var result = await _goodsReceiptRepo.GetAllGoodsReceipt();

            return result;
        }

        public async Task<bool> UpdateGoodsReceipt(GoodsReceipt model)
        {
            var result = await _goodsReceiptRepo.UpdateGoodsReceipt(model);

            return result;
        }

        public async Task<bool> DeleteGoodsReceipt(int id)
        {
            var result = await _goodsReceiptRepo.DeleteGoodsReceipt(id);

            return result;
        }
    }
}