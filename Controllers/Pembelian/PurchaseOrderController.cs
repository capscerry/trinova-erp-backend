using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly IPurchaseOrderUsecase _purchaseOrderUsecase;

        public PurchaseOrderController(
            IPurchaseOrderUsecase purchaseOrderUsecase
        )
        {
            _purchaseOrderUsecase = purchaseOrderUsecase;
        }

        [HttpPost("/api/purchase-order")]
        public async Task<IActionResult> InsertPurchaseOrder(
            [FromBody] PurchaseOrder purchaseOrder
        )
        {
            var result = await _purchaseOrderUsecase
                .InsertPurchaseOrder(purchaseOrder);

            if (result > 0)
            {
                return Ok(new
                {
                    status = true,
                    message = "Insert Successfully",
                    purchase_order_id = result
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Insert Failed"
            });
        }

        [HttpGet("/api/purchase-order/next-number")]
        public async Task<IActionResult> GetNextPONumber()
        {
            var number = await _purchaseOrderUsecase.GetNextPONumber();
            
            return Ok(new
            {
                status = true,
                po_number = number,
                next_number = number
            });
        }

        [HttpGet("/api/purchase-order")]
        public async Task<IActionResult> GetAllPurchaseOrder()
        {
            var result = await _purchaseOrderUsecase
                .GetAllPurchaseOrder();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Purchase Order Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/purchase-order/{id}")]
        public async Task<IActionResult> UpdatePurchaseOrder(
            int id,
            [FromBody] PurchaseOrder model
        )
        {
            model.purchase_order_id = id;

            var result = await _purchaseOrderUsecase
                .UpdatePurchaseOrder(model);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Data"
            });
        }

        [HttpDelete("/api/purchase-order/{id}")]
        public async Task<IActionResult> DeletePurchaseOrder(int id)
        {
            var result = await _purchaseOrderUsecase
                .DeletePurchaseOrder(id);

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