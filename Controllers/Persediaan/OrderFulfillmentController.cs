// Controllers/Persediaan/OrderFulfillmentController.cs

using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan.DTO;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class OrderFulfillmentController
        : ControllerBase
    {
        private readonly OrderFulfillmentUsecase _usecase;

        public OrderFulfillmentController(
            OrderFulfillmentUsecase usecase)
        {
            _usecase = usecase;
        }

        [HttpPost]
        public async Task<IActionResult> Fulfill(
            OrderFulfillmentRequest request)
        {
            await _usecase.Fulfill(request);

            return Ok(new
            {
                success = true,
                message = "Order fulfilled successfully"
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(
                await _usecase.GetAllAsync()
            );
        }

        [HttpPut("{movementId}/complete")]
        public async Task<IActionResult> Complete(int movementId)
        {
            await _usecase.CompleteAsync(movementId);

            return Ok(new
            {
                success = true,
                message = "Order fulfillment completed successfully."
            });
        }

        [HttpPut("{movementId}/cancel")]
        public async Task<IActionResult> Cancel(int movementId)
        {
            await _usecase.CancelAsync(movementId);

            return Ok(new
            {
                success = true,
                message = "Order fulfillment canceled successfully."
            });
        }
    }
}
