using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    // DTO for settlement updates (status, notes, and closing_condition are mutable after creation)
    public class PurchaseReturnUpdateRequest
    {
        public string? status            { get; set; }
        public string? notes             { get; set; }
        public string? closing_condition { get; set; }

        /// <summary>
        /// Optional. When the Cash Refund settlement dialog lets the user pick
        /// a specific invoice, pass its purchase_invoice_id here so the credit
        /// is applied to exactly that invoice. If omitted the system falls back
        /// to the oldest eligible invoice for the supplier (legacy behaviour).
        /// </summary>
        public int? target_invoice_id    { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseReturnController : ControllerBase
    {
        private readonly IPurchaseReturnUsecase _purchaseReturnUsecase;

        public PurchaseReturnController(
            IPurchaseReturnUsecase purchaseReturnUsecase
        )
        {
            _purchaseReturnUsecase = purchaseReturnUsecase;
        }

        [HttpGet("/api/purchase-return/next-number")]
        public async Task<IActionResult> GetNextReturnNumber()
        {
            var number = await _purchaseReturnUsecase.GetNextReturnNumber();

            return Ok(new
            {
                status = true,
                next_number = number
            });
        }

        [HttpPost("/api/purchase-return")]
        public async Task<IActionResult> InsertPurchaseReturn(
            [FromBody] PurchaseReturn purchaseReturn
        )
        {
            try
            {
                var id = await _purchaseReturnUsecase
                    .InsertPurchaseReturn(purchaseReturn);

                return Ok(new
                {
                    status = true,
                    purchase_return_id = id
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

        [HttpGet("/api/purchase-return")]
        public async Task<IActionResult> GetAllPurchaseReturn()
        {
            var result = await _purchaseReturnUsecase.GetAllPurchaseReturn();

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        /// <summary>
        /// Returns the product lines (product_name + quantity) for the Goods
        /// Receipt linked to this purchase return.
        /// The Accept Loss modal calls this to display the correct items and
        /// quantities that will be restored when the settlement is confirmed.
        /// </summary>
        [HttpGet("/api/purchase-return/{id}/details")]
        public async Task<IActionResult> GetReturnDetails(int id)
        {
            try
            {
                var result = await _purchaseReturnUsecase.GetReturnDetails(id);

                return Ok(new
                {
                    status = true,
                    data = result
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

        /// <summary>
        /// Returns only the GR detail lines where remaining_qty &gt; 0 for the
        /// given goods_receipt_id.  The Purchase Return creation form calls
        /// this to populate its product selection table, ensuring the user
        /// only sees items that can still be returned.
        ///
        /// Each line carries:
        ///   product_name   — display name
        ///   quantity       — original received qty (read-only reference)
        ///   remaining_qty  — maximum the user may enter as Return Qty
        /// </summary>
        [HttpGet("/api/purchase-return/gr/{grId}/available-details")]
        public async Task<IActionResult> GetAvailableReturnDetails(int grId)
        {
            try
            {
                var result = await _purchaseReturnUsecase
                    .GetAvailableReturnDetails(grId);

                return Ok(new
                {
                    status = true,
                    data = result
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

        /// <summary>
        /// Returns all unpaid / partially-paid invoices for the supplier linked
        /// to this purchase return. The supplier is resolved internally via the
        /// return's goods_receipt_id -> purchase_order -> supplier_id chain,
        /// so the frontend only needs the purchase_return_id.
        /// Each invoice carries a real-time outstanding_amount.
        ///
        /// <c>cash_refund_available</c> is <c>true</c> when at least one
        /// outstanding invoice exists. The frontend should lock Option C
        /// (Cash Refund) and force the user to pick Option A or B when this
        /// is <c>false</c>.
        /// </summary>
        [HttpGet("/api/purchase-return/{id}/invoices")]
        public async Task<IActionResult> GetUnpaidInvoicesForReturn(int id)
        {
            try
            {
                var result = await _purchaseReturnUsecase
                    .GetUnpaidInvoicesForReturn(id);

                return Ok(new
                {
                    status               = true,
                    cash_refund_available = result.CashRefundAvailable,
                    data                 = result.Invoices
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status  = false,
                    message = ex.Message
                });
            }
        }

        [HttpPut("/api/purchase-return/{id}")]
        public async Task<IActionResult> UpdatePurchaseReturn(
            int id,
            [FromBody] PurchaseReturnUpdateRequest request
        )
        {
            try
            {
                var result = await _purchaseReturnUsecase
                    .UpdatePurchaseReturn(
                        id,
                        request.status            ?? "",
                        request.notes             ?? "",
                        request.closing_condition ?? "",
                        request.target_invoice_id
                    );

                if (!result)
                    return BadRequest(new
                    {
                        status = false,
                        message = "Update failed or record not found"
                    });

                return Ok(new
                {
                    status = true,
                    message = "Purchase Return updated successfully"
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

        [HttpDelete("/api/purchase-return/{id}")]
        public async Task<IActionResult> DeletePurchaseReturn(int id)
        {
            var result = await _purchaseReturnUsecase.DeletePurchaseReturn(id);

            if (!result)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Purchase Return cannot be deleted"
                });
            }

            return Ok(new
            {
                status = true,
                message = "Purchase Return deleted successfully"
            });
        }
    }
}
