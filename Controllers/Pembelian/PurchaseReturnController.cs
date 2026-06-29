using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    // DTO for settlement updates (only status + notes are mutable after creation)
    public class PurchaseReturnUpdateRequest
    {
        public string? status { get; set; }
        public string? notes  { get; set; }
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
                        request.status ?? "",
                        request.notes  ?? ""
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
