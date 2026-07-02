using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian")]
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
            catch (Exception ex)
            {
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
            purchaseInvoice.purchase_invoice_id =
                id;

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
                    message =
                        "Purchase Invoice cannot be updated"
                });
            }

            return Ok(new
            {
                status = true,
                message =
                    "Purchase Invoice updated successfully"
            });
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
    }
}
