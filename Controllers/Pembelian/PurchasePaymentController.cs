using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian")]
    public class PurchasePaymentController
        : ControllerBase
    {
        private readonly
            IPurchasePaymentUsecase
            _purchasePaymentUsecase;

        public PurchasePaymentController(
            IPurchasePaymentUsecase
                purchasePaymentUsecase
        )
        {
            _purchasePaymentUsecase =
                purchasePaymentUsecase;
        }

        [HttpPost("/api/purchase-payment")]
        public async Task<IActionResult>
            InsertPurchasePayment(
                [FromBody]
                PurchasePayment purchasePayment
            )
        {
            try
            {
                var paymentId =
                    await _purchasePaymentUsecase
                        .InsertPurchasePayment(
                            purchasePayment
                        );

                return Ok(new
                {
                    status = true,
                    purchase_payment_id =
                        paymentId
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

        [HttpGet("/api/purchase-payment/next-number")]
        public async Task<IActionResult> GetNextPaymentNumber()
        {
            var number = await _purchasePaymentUsecase
                .GetNextPaymentNumber();

            return Ok(new
            {
                status = true,
                payment_number = number,
                next_number = number
            });
        }

        [HttpGet("/api/purchase-payment")]
        public async Task<IActionResult>
            GetAllPurchasePayment()
        {
            var result =
                await _purchasePaymentUsecase
                    .GetAllPurchasePayment();

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/purchase-payment/{id}")]
        public async Task<IActionResult>
            UpdatePurchasePayment(
                int id,
                [FromBody]
                PurchasePayment purchasePayment
            )
        {
            try
            {
                var result =
                    await _purchasePaymentUsecase
                        .UpdatePurchasePayment(
                            id,
                            purchasePayment
                        );

                if (!result)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Purchase Payment not found or could not be updated"
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Purchase Payment updated successfully"
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

        [HttpDelete("/api/purchase-payment/{id}")]
        public async Task<IActionResult>
            DeletePurchasePayment(
                int id
            )
        {
            var result =
                await _purchasePaymentUsecase
                    .DeletePurchasePayment(
                        id
                    );

            if (!result)
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Purchase Payment cannot be deleted"
                });
            }

            return Ok(new
            {
                status = true,
                message =
                    "Purchase Payment deleted successfully"
            });
        }
    }
}
