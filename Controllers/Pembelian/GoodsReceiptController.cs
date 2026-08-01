using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
    public class GoodsReceiptController : ControllerBase
    {
        private readonly IGoodsReceiptUsecase _goodsReceiptUsecase;
        private readonly ILogger<GoodsReceiptController> _logger;

        public GoodsReceiptController(
            IGoodsReceiptUsecase goodsReceiptUsecase,
            ILogger<GoodsReceiptController> logger
        )
        {
            _goodsReceiptUsecase = goodsReceiptUsecase;
            _logger = logger;
        }

        [HttpPost("/api/goods-receipt")]
        public async Task<IActionResult> InsertGoodsReceipt(
            [FromBody] GoodsReceipt goodsReceipt
        )
        {
            try
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
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
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

        /// <summary>
        /// Returns only Goods Receipts that have at least one detail line
        /// with remaining_qty &gt; 0. This is the list the Purchase Return
        /// creation modal must use — exhausted GRs are excluded entirely.
        /// </summary>
        [HttpGet("/api/goods-receipt/for-purchase-return")]
        public async Task<IActionResult> GetAllAvailableForReturn()
        {
            try
            {
                var result = await _goodsReceiptUsecase
                    .GetAllAvailableForReturn();

                return Ok(new
                {
                    status = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to load Goods Receipts available for Purchase Return.");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = false,
                    message = "Gagal memuat daftar Goods Receipt untuk retur."
                });
            }
        }

        /// <summary>
        /// Updates a Goods Receipt header (receipt_number, receipt_date,
        /// received_by, status).  Fixes the 405 that was triggered when the
        /// frontend called PUT /goods-receipt/{id} and found no matching route.
        /// </summary>
        [HttpPut("/api/goods-receipt/{id}")]
        public async Task<IActionResult> UpdateGoodsReceipt(
            int id,
            [FromBody] GoodsReceipt goodsReceipt
        )
        {
            goodsReceipt.goods_receipt_id = id;

            var result = await _goodsReceiptUsecase
                .UpdateGoodsReceipt(goodsReceipt);

            if (!result)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Update Failed"
                });
            }

            return Ok(new
            {
                status = true,
                message = "Goods Receipt updated successfully"
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
