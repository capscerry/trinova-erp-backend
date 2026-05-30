using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
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