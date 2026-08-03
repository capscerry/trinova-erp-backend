using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Security;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.PurchasingAndInventoryAccess)]
    public class PurchaseRequisitionController : ControllerBase
    {
        private readonly PurchaseRequisitionUsecase _purchaseRequisitionUsecase;

        public PurchaseRequisitionController(
            PurchaseRequisitionUsecase purchaseRequisitionUsecase
        )
        {
            _purchaseRequisitionUsecase = purchaseRequisitionUsecase;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result =
                await _purchaseRequisitionUsecase.GetAllAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var result =
                await _purchaseRequisitionUsecase.GetDetailAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result =
                await _purchaseRequisitionUsecase.GetDetailAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] PurchaseRequisition model
        )
        {
            Console.WriteLine(
            $"DETAIL COUNT = {model.Details?.Count}"
        );

        return Ok(
            await _purchaseRequisitionUsecase.CreateAsync(model)
        );
        }

        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextPRNumber()
        {
            var number = await _purchaseRequisitionUsecase
                .GetNextPRNumber();

            return Ok(new
            {
                status = true,
                pr_number = number,
                next_number = number
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] PurchaseRequisition model
        )
        {
            var success =
                await _purchaseRequisitionUsecase.UpdateAsync(id, model);

            if (!success)
                return NotFound();

            return Ok(new
            {
                message = "Purchase Requisition updated successfully"
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success =
                await _purchaseRequisitionUsecase.DeleteAsync(id);

            if (!success)
                return NotFound();

            return Ok(new
            {
                message = "Purchase Requisition deleted successfully"
            });
        }
    }
}
