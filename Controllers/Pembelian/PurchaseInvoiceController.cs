using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
    public class PurchaseInvoiceController : ControllerBase
    {
        private readonly IPurchaseInvoiceUsecase
            _purchaseInvoiceUsecase;

        public PurchaseInvoiceController(
            IPurchaseInvoiceUsecase purchaseInvoiceUsecase
        )
        {
            _purchaseInvoiceUsecase =
                purchaseInvoiceUsecase;
        }

        [HttpPost("/api/purchase-invoice")]
        public async Task<IActionResult>
            InsertPurchaseInvoice(
                [FromBody]
                PurchaseInvoice purchaseInvoice
            )
        {
            try
            {
                var purchaseInvoiceId =
                    await _purchaseInvoiceUsecase
                        .InsertPurchaseInvoice(
                            purchaseInvoice
                        );

                return Ok(new
                {
                    status = true,
                    purchase_invoice_id =
                        purchaseInvoiceId
                });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate invoice for the same Goods Receipt
                return Conflict(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Check if the message is a known duplicate-record message
                // thrown as a plain Exception from the usecase layer.
                if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("Invoice already", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new
                    {
                        status = false,
                        message = ex.Message
                    });
                }

                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/api/purchase-invoice/next-number")]
        public async Task<IActionResult> GetNextInvoiceNumber()
        {
            var number = await _purchaseInvoiceUsecase
                .GetNextInvoiceNumber();

            return Ok(new
            {
                status = true,
                invoice_number = number,
                next_number = number
            });
        }

        [HttpGet("/api/purchase-invoice")]
        public async Task<IActionResult>
            GetAllPurchaseInvoice()
        {
            var result =
                await _purchaseInvoiceUsecase
                    .GetAllPurchaseInvoice();

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        /// <summary>
        /// Returns all unpaid / partially-paid invoices for a specific supplier
        /// with their real-time outstanding_amount. Call this when opening the
        /// Purchase Return settlement dialog so the dropdown always reflects
        /// the current balance rather than a cached value.
        /// </summary>
        [HttpGet("/api/purchase-invoice/unpaid/{supplierId}")]
        public async Task<IActionResult>
            GetUnpaidInvoicesBySupplier(int supplierId)
        {
            try
            {
                var result =
                    await _purchaseInvoiceUsecase
                        .GetUnpaidInvoicesBySupplier(supplierId);

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

        [HttpPut("/api/purchase-invoice/{id}")]
        public async Task<IActionResult>
            UpdatePurchaseInvoice(
                int id,
                [FromBody]
                PurchaseInvoice purchaseInvoice
            )
        {
            try
            {
                purchaseInvoice.purchase_invoice_id = id;

                var result =
                    await _purchaseInvoiceUsecase
                        .UpdatePurchaseInvoice(
                            purchaseInvoice
                        );

                if (!result)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Purchase Invoice cannot be updated"
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Purchase Invoice updated successfully"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
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

        [HttpDelete("/api/purchase-invoice/{id}")]
        public async Task<IActionResult>
            DeletePurchaseInvoice(
                int id
            )
        {
            var result =
                await _purchaseInvoiceUsecase
                    .DeletePurchaseInvoice(
                        id
                    );

            if (!result)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Purchase Invoice cannot be deleted"
                });
            }

            return Ok(new
            {
                status = true,
                message =
                    "Purchase Invoice deleted successfully"
            });
        }

        /// <summary>
        /// Recalculates outstanding_amount for every non-Cancelled invoice
        /// and bulk-updates their status to Paid/Unpaid.
        /// Call this once from the frontend on page load to fix any invoices
        /// whose status was not updated by prior payment operations.
        /// </summary>
        [HttpPost("/api/purchase-invoice/sync-status")]
        public async Task<IActionResult> SyncAllInvoiceStatuses()
        {
            try
            {
                await _purchaseInvoiceUsecase.SyncAllInvoiceStatuses();
                return Ok(new { status = true, message = "Invoice statuses synced successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }
    }
}
