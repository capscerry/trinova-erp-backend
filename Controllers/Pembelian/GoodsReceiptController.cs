using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian")]
    public class GoodsReceiptController : ControllerBase
    {
        private readonly IGoodsReceiptUsecase _goodsReceiptUsecase;

        public GoodsReceiptController(
            IGoodsReceiptUsecase goodsReceiptUsecase
        )
        {
            _goodsReceiptUsecase = goodsReceiptUsecase;
        }

        [HttpPost("/api/goods-receipt")]
        public async Task<IActionResult> InsertGoodsReceipt(
            [FromBody] GoodsReceipt goodsReceipt
        )
        {
            var goodsReceiptId =
                await _goodsReceiptUsecase
                    .InsertGoodsReceipt(goodsReceipt);

            if (goodsReceiptId <= 0)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Insert Failed"
                });
            }

            return Ok(new
            {
                status = true,
                goods_receipt_id = goodsReceiptId
            });
        }

        [HttpGet("/api/goods-receipt/next-number")]
        public async Task<IActionResult> GetNextGRNumber()
        {
            var number = await _goodsReceiptUsecase.GetNextGRNumber();

            return Ok(new
            {
                status = true,
                receipt_number = number,
                next_number = number
            });
        }

        [HttpGet("/api/goods-receipt")]
        public async Task<IActionResult> GetAllGoodsReceipt()
        {
            var result = await _goodsReceiptUsecase
                .GetAllGoodsReceipt();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Goods Receipt Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpGet("/api/goods-receipt/without-invoice")]
        public async Task<IActionResult> GetAllWithoutInvoice()
        {
            var result = await _goodsReceiptUsecase
                .GetAllWithoutInvoice();

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpDelete("/api/goods-receipt/{id}")]
        public async Task<IActionResult> DeleteGoodsReceipt(int id)
        {
            var result = await _goodsReceiptUsecase
                .DeleteGoodsReceipt(id);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Delete Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Delete Data"
            });
        }
    }
}
