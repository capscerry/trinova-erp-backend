using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseDownPaymentController
        : ControllerBase
    {
        private readonly
            IPurchaseDownPaymentUsecase
            _purchaseDownPaymentUsecase;

        public PurchaseDownPaymentController(
            IPurchaseDownPaymentUsecase
                purchaseDownPaymentUsecase
        )
        {
            _purchaseDownPaymentUsecase =
                purchaseDownPaymentUsecase;
        }

        [HttpPost("/api/purchase-down-payment")]
        public async Task<IActionResult>
            InsertPurchaseDownPayment(
                [FromBody]
                PurchaseDownPayment model
            )
        {
            var result =
                await _purchaseDownPaymentUsecase
                    .InsertPurchaseDownPayment(
                        model
                    );

            if (result > 0)
            {
                return Ok(
                    new
                    {
                        status = true,
                        message =
                            "Insert Successfully",

                        purchase_down_payment_id =
                            result
                    }
                );
            }

            return BadRequest(
                new
                {
                    status = false,
                    message =
                        "Insert Failed"
                }
            );
        }

        [HttpGet("/api/purchase-down-payment/next-number")]
        public async Task<IActionResult> GetNextDPNumber()
        {
            var number = await _purchaseDownPaymentUsecase
                .GetNextDPNumber();

            return Ok(new
            {
                status = true,
                dp_number = number,
                next_number = number
            });
        }

        [HttpGet("/api/purchase-down-payment")]
        public async Task<IActionResult>
            GetAllPurchaseDownPayment()
        {
            var result =
                await _purchaseDownPaymentUsecase
                    .GetAllPurchaseDownPayment();

            return Ok(
                new
                {
                    status = true,
                    data = result
                }
            );
        }
    }
}