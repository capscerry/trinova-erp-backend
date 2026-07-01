using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/goods-receipt-detail")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian")]
    public class GoodsReceiptDetailController : ControllerBase
    {
        private readonly IGoodsReceiptDetailUsecase _goodsReceiptDetailUsecase;

        public GoodsReceiptDetailController(
            IGoodsReceiptDetailUsecase goodsReceiptDetailUsecase
        )
        {
            _goodsReceiptDetailUsecase = goodsReceiptDetailUsecase;
        }

        [HttpPost]
        public async Task<IActionResult> InsertGoodsReceiptDetail(
            [FromBody] GoodsReceiptDetail goodsReceiptDetail
        )
        {
            var result = await _goodsReceiptDetailUsecase
                .InsertGoodsReceiptDetail(goodsReceiptDetail);

            return Ok(new
            {
                status = true,
                message = result
            });
        }
    }
}
