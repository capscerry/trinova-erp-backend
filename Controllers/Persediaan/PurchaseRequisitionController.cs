using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> GetAll()
        {
            var result =
                await _purchaseRequisitionUsecase.GetAllAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result =
                await _purchaseRequisitionUsecase.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] PurchaseRequisition model
        )
        {
            var result =
                await _purchaseRequisitionUsecase.CreateAsync(model);

            return Ok(result);
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