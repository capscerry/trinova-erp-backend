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

        /// <summary>
        /// Header + item lines for one Goods Receipt. Item price/subtotal
        /// come from the source Purchase Order's detail lines (matched by
        /// product_id) since goods_receipt_detail itself does not store
        /// price -- mirrors what the old detail modal displayed.
        /// </summary>
        Task<object?> GetGoodsReceiptById(int id);
    }

    public class GoodsReceiptUsecase : IGoodsReceiptUsecase
    {
        private readonly IGoodsReceiptRepo _goodsReceiptRepo;
        private readonly IPurchaseOrderRepo _purchaseOrderRepo;
        private readonly IGoodsReceiptDetailRepo _goodsReceiptDetailRepo;
        private readonly IPurchaseOrderDetailRepo _purchaseOrderDetailRepo;

        public GoodsReceiptUsecase(
            IGoodsReceiptRepo goodsReceiptRepo,
            IPurchaseOrderRepo purchaseOrderRepo,
            IGoodsReceiptDetailRepo goodsReceiptDetailRepo,
            IPurchaseOrderDetailRepo purchaseOrderDetailRepo
        )
        {
            _goodsReceiptRepo = goodsReceiptRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
            _goodsReceiptDetailRepo = goodsReceiptDetailRepo;
            _purchaseOrderDetailRepo = purchaseOrderDetailRepo;
        }

        public async Task<string> GetNextGRNumber()
        {
            return await _goodsReceiptRepo.GenerateGRNumber();
        }

        public async Task<int> InsertGoodsReceipt(GoodsReceipt model)
        {
            bool isExist = await _goodsReceiptRepo
                .IsGoodsReceiptExist(model.purchase_order_id);

            if (isExist)
            {
                throw new InvalidOperationException(
                    "Goods Receipt already exists for this Purchase Order."
                );
            }

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

        public async Task<object?> GetGoodsReceiptById(int id)
        {
            var header = await _goodsReceiptRepo.GetGoodsReceiptById(id);
            if (header == null) return null;

            var grItems = await _goodsReceiptDetailRepo
                .GetDetailsByGoodsReceiptIdWithProductName(id);

            // Price/subtotal live on the source PO's detail lines --
            // goods_receipt_detail has no price column of its own.
            var poItems = await _purchaseOrderDetailRepo
                .GetDetailsByPurchaseOrderId(header.purchase_order_id);
            var priceByProduct = poItems
                .GroupBy(p => p.product_id)
                .ToDictionary(g => g.Key, g => g.First().price);

            return new
            {
                header.goods_receipt_id,
                header.purchase_order_id,
                header.receipt_number,
                header.receipt_date,
                header.received_by,
                header.status,
                header.supplier_id,
                header.supplier_name,
                header.po_number,
                header.total_amount,
                header.transaction_name,
                header.transaction_detail,
                header.nomor_faktur_pajak,
                items = grItems.Select(i =>
                {
                    var price = priceByProduct.TryGetValue(i.product_id, out var p) ? p : 0;
                    return new
                    {
                        i.goods_receipt_detail_id,
                        i.product_id,
                        i.product_name,
                        i.quantity,
                        price,
                        subtotal = price * i.quantity,
                    };
                })
            };
        }
    }
}