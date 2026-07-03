using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian")]
    public class PurchaseOrderDetailController : ControllerBase
    {
        private readonly IPurchaseOrderDetailUsecase _purchaseOrderDetailUsecase;

        public PurchaseOrderDetailController(
            IPurchaseOrderDetailUsecase purchaseOrderDetailUsecase
        )
        {
            _purchaseOrderDetailUsecase = purchaseOrderDetailUsecase;
        }

        [HttpPost("/api/purchase-order-detail")]
        public async Task<IActionResult> InsertPurchaseOrderDetail(
            [FromBody] PurchaseOrderDetail detail
        )
        {
            var result = await _purchaseOrderDetailUsecase
                .InsertPurchaseOrderDetail(detail);

            if (result == "Insert Successfully")
            {
                return Ok(new
                {
                    status = true,
                    message = result
                });
            }

            return BadRequest(new
            {
                status = false,
                message = result
            });
        }

        [HttpGet("/api/purchase-order-detail")]
        public async Task<IActionResult> GetAllPurchaseOrderDetail()
        {
            var result = await _purchaseOrderDetailUsecase
                .GetAllPurchaseOrderDetail();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Purchase Order Detail Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/purchase-order-detail/{id}")]
        public async Task<IActionResult> UpdatePurchaseOrderDetail(
            int id,
            [FromBody] PurchaseOrderDetail model
        )
        {
            model.purchase_order_detail_id = id;

            var result = await _purchaseOrderDetailUsecase
                .UpdatePurchaseOrderDetail(model);

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

        [HttpDelete("/api/purchase-order-detail/{id}")]
        public async Task<IActionResult> DeletePurchaseOrderDetail(int id)
        {
            var result = await _purchaseOrderDetailUsecase
                .DeletePurchaseOrderDetail(id);

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
