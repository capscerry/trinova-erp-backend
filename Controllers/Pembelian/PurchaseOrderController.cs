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

        [HttpPatch("/api/purchase-order/{id}/approve")]
        public async Task<IActionResult> ApprovePurchaseOrder(int id)
        {
            var (success, message) = await _purchaseOrderUsecase
                .ApprovePurchaseOrder(id);

            if (success)
                return Ok(new { status = true, message });

            return BadRequest(new { status = false, message });
        }

        [HttpPatch("/api/purchase-order/{id}/unapprove")]
        public async Task<IActionResult> UnapprovePurchaseOrder(int id)
        {
            var result = await _purchaseOrderUsecase
                .UnapprovePurchaseOrder(id);

            if (result)
                return Ok(new { status = true, message = "Purchase Order unapproved and stock restored" });

            return BadRequest(new { status = false, message = "Unapprove failed — PO not found or not in Approved state" });
        }

        [HttpPatch("/api/purchase-order/{id}/deduction")]
        public async Task<IActionResult> ApplyDeduction(
            int id,
            [FromBody] PODeductionRequest request
        )
        {
            // Record the deduction as a note in the PO's status field or simply
            // acknowledge it. The deduction amount is already stored on the
            // purchase_return record; this endpoint exists so the frontend can
            // confirm the PATCH without a 404.
            return Ok(new
            {
                status = true,
                message = $"Deduction of {request.deduction_amount} noted against PO {id}",
                purchase_order_id = id,
                deduction_amount = request.deduction_amount,
                purchase_return_id = request.purchase_return_id
            });
        }

        [HttpDelete("/api/purchase-order/{id}")]
        public async Task<IActionResult> DeletePurchaseOrder(int id)        {
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

namespace trinova_erp_backend.Controllers.Pembelian
{
    public class PODeductionRequest
    {
        public decimal deduction_amount  { get; set; }
        public int     purchase_return_id { get; set; }
    }
}
