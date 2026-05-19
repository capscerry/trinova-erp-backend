using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IGoodsReceiptDetailUsecase
    {
        Task<string> InsertGoodsReceiptDetail(GoodsReceiptDetail model);
    }

    public class GoodsReceiptDetailUsecase : IGoodsReceiptDetailUsecase
    {
        private readonly IGoodsReceiptDetailRepo _goodsReceiptDetailRepo;

        public GoodsReceiptDetailUsecase(
            IGoodsReceiptDetailRepo goodsReceiptDetailRepo
        )
        {
            _goodsReceiptDetailRepo = goodsReceiptDetailRepo;
        }

        public async Task<string> InsertGoodsReceiptDetail(GoodsReceiptDetail model)
        {
            var result = await _goodsReceiptDetailRepo
                .InsertGoodsReceiptDetail(model);

            return result
                ? "Insert Successfully"
                : "Insert Failed";
        }
    }
}