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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(
                await _usecase.GetAllAsync()
            );
        }
    }
}
