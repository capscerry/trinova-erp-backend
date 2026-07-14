using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan.DTO;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class StockTransferController : ControllerBase
    {
        private readonly StockTransferUsecase _usecase;

        public StockTransferController(
            StockTransferUsecase usecase)
        {
            _usecase = usecase;
        }

        [HttpPost]
        public async Task<IActionResult> Transfer(
            StockTransferRequest request)
        {
            await _usecase.Transfer(request);

            return Ok(new
            {
                success = true,
                message = "Stock transferred successfully"
            });
        }

        [HttpGet("next-number")]
        public async Task<IActionResult> GetNextTRFNumber()
        {
            var number = await _usecase.GetNextTRFNumber();

            return Ok(new
            {
                status = true,
                reference_number = number,
                next_number = number
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(
                await _usecase.GetAllAsync()
            );
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _usecase.GetDetail(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPut("{id}/process")]
        public async Task<IActionResult> Process(int id)
        {
            await _usecase.ProcessAsync(id);

            return Ok(new
            {
                success = true,
                message = "Transfer processed successfully."
            });
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            await _usecase.CompleteAsync(id);

            return Ok(new
            {
                success = true,
                message = "Transfer completed successfully."
            });
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _usecase.CancelAsync(id);

            return Ok(new
            {
                message = "Transfer canceled."
            });
        }
    }
}
