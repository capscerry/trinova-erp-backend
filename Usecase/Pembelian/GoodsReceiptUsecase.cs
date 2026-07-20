using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IGoodsReceiptUsecase
    {
        Task<string> GetNextGRNumber();

        Task<int> InsertGoodsReceipt(GoodsReceipt model);

        Task<List<GoodsReceipt>> GetAllGoodsReceipt();

        Task<List<GoodsReceipt>> GetAllWithoutInvoice();

        /// <summary>
        /// Returns only GRs that have at least one detail line with
        /// remaining_qty &gt; 0. Used by the Purchase Return creation modal.
        /// </summary>
        Task<List<GoodsReceipt>> GetAllAvailableForReturn();

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

        public async Task<string> GetNextGRNumber()
        {
            return await _goodsReceiptRepo.GenerateGRNumber();
        }

        public async Task<int> InsertGoodsReceipt(GoodsReceipt model)
        {
            model.created_at = DateTime.Now;
            model.status = "Received";
            model.receipt_number =
                await _goodsReceiptRepo.GenerateGRNumber();

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

        public async Task<List<GoodsReceipt>> GetAllWithoutInvoice()
        {
            return await _goodsReceiptRepo.GetAllWithoutInvoice();
        }

        public async Task<List<GoodsReceipt>> GetAllAvailableForReturn()
        {
            return await _goodsReceiptRepo.GetAllAvailableForReturn();
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