using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    // All endpoints require authentication at minimum.
    // Individual endpoints carry the narrowest role list needed.
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly IPurchaseOrderUsecase         _purchaseOrderUsecase;
        private readonly ILogger<PurchaseOrderController> _logger;

        public PurchaseOrderController(
            IPurchaseOrderUsecase purchaseOrderUsecase,
            ILogger<PurchaseOrderController> logger
        )
        {
            _purchaseOrderUsecase = purchaseOrderUsecase;
            _logger               = logger;
        }

        // ── PURCHASING-ONLY ENDPOINTS ────────────────────────────────────
        // Create, edit, delete, and draft-number generation remain restricted
        // to Purchasing staff. Procurement Manager has no access here.

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
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

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
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

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpGet("/api/purchase-order/{id}/detail")]
        public async Task<IActionResult> GetPurchaseOrderPrintDetail(int id)
        {
            try
            {
                var result = await _purchaseOrderUsecase.GetPurchaseOrderPrintDetailAsync(id);

                if (result == null)
                    return NotFound(new { status = false, message = "Purchase Order tidak ditemukan." });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpPost("/api/purchase-order/{id}/send-email")]
        public async Task<IActionResult> SendPurchaseOrderEmail(int id, [FromBody] SendQuotationEmailRequest? request)
        {
            _logger.LogInformation("[SendEmail] Controller received request — PO id={Id}", id);
            try
            {
                await _purchaseOrderUsecase.SendPurchaseOrderEmailAsync(id, request);
                _logger.LogInformation("[SendEmail] Controller — completed successfully, PO id={Id}", id);
                return Ok(new { status = true, message = "Email Purchase Order (PDF) berhasil dikirim ke supplier." });
            }
            catch (InvalidOperationException ex)
            {
                // [DIAGNOSTIC] Full exception detail for InvalidOperationException
                _logger.LogError(ex,
                    "[SendEmail] InvalidOperationException — ExceptionType: {Type} | Message: {Message} | " +
                    "InnerExceptionType: {InnerType} | InnerMessage: {InnerMessage} | StackTrace: {StackTrace}",
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.GetType().FullName ?? "(none)",
                    ex.InnerException?.Message          ?? "(none)",
                    ex.StackTrace);
                return BadRequest(new { status = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                // [DIAGNOSTIC] Full exception detail for ArgumentException
                _logger.LogError(ex,
                    "[SendEmail] ArgumentException — ExceptionType: {Type} | Message: {Message} | " +
                    "InnerExceptionType: {InnerType} | InnerMessage: {InnerMessage} | StackTrace: {StackTrace}",
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.GetType().FullName ?? "(none)",
                    ex.InnerException?.Message          ?? "(none)",
                    ex.StackTrace);
                return BadRequest(new { status = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // [DIAGNOSTIC] Full exception detail for any unexpected exception
                _logger.LogError(ex,
                    "[SendEmail] Unexpected Exception — ExceptionType: {Type} | Message: {Message} | " +
                    "InnerExceptionType: {InnerType} | InnerMessage: {InnerMessage} | StackTrace: {StackTrace}",
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.GetType().FullName ?? "(none)",
                    ex.InnerException?.Message          ?? "(none)",
                    ex.StackTrace);
                return StatusCode(500, new { status = false, message = "Terjadi kesalahan tak terduga: " + ex.Message });
            }
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
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

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
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

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpPatch("/api/purchase-order/{id}/request-approval")]
        public async Task<IActionResult> RequestApproval(int id)
        {
            var (success, message) = await _purchaseOrderUsecase
                .RequestApproval(id);

            if (success)
                return Ok(new { status = true, message });

            return BadRequest(new { status = false, message });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpPatch("/api/purchase-order/{id}/unapprove")]
        public async Task<IActionResult> UnapprovePurchaseOrder(int id)
        {
            var result = await _purchaseOrderUsecase
                .UnapprovePurchaseOrder(id);

            if (result)
                return Ok(new { status = true, message = "Purchase Order unapproved and stock restored" });

            return BadRequest(new { status = false, message = "Unapprove failed — PO not found or not in Approved state" });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpPatch("/api/purchase-order/{id}/deduction")]
        public async Task<IActionResult> ApplyDeduction(
            int id,
            [FromBody] PODeductionRequest request
        )
        {
            var po = await _purchaseOrderUsecase.GetPurchaseOrderById(id);

            if (po == null)
                return NotFound(new
                {
                    status = false,
                    message = "Purchase Order not found."
                });

            decimal poTotal = po.total_amount ?? 0m;

            if (request.deduction_amount > poTotal)
                return BadRequest(new
                {
                    status = false,
                    message =
                        $"PO Deduction cannot be applied: the return value " +
                        $"(Rp {request.deduction_amount:N0}) exceeds the PO total " +
                        $"(Rp {poTotal:N0}). Please select a PO whose total is at " +
                        $"least equal to the return amount."
                });

            return Ok(new
            {
                status = true,
                message = $"Deduction of {request.deduction_amount} noted against PO {id}",
                purchase_order_id = id,
                deduction_amount = request.deduction_amount,
                purchase_return_id = request.purchase_return_id
            });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
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


        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpGet("/api/purchase-order/pending-approval")]
        public async Task<IActionResult> GetPendingApproval()
        {
            var result = await _purchaseOrderUsecase
                .GetPurchaseOrdersByStatus("Waiting for Approval");

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Purchase Orders pending approval"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpGet("/api/purchase-order/approved")]
        public async Task<IActionResult> GetApproved()
        {
            var result = await _purchaseOrderUsecase
                .GetApprovedAndCompletedPurchaseOrders();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Approved Purchase Orders found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpPatch("/api/purchase-order/{id}/approve")]
        public async Task<IActionResult> ApprovePurchaseOrder(int id)
        {
            var (success, message) = await _purchaseOrderUsecase
                .ApprovePurchaseOrder(id);

            if (success)
                return Ok(new { status = true, message });

            return BadRequest(new { status = false, message });
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
        [HttpPatch("/api/purchase-order/{id}/reject")]
        public async Task<IActionResult> RejectPurchaseOrder(int id)
        {
            var (success, message) = await _purchaseOrderUsecase
                .RejectPurchaseOrder(id);

            if (success)
                return Ok(new { status = true, message });

            return BadRequest(new { status = false, message });
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
