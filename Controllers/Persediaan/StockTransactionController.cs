using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class StockTransactionController : ControllerBase
    {
        private readonly StockTransactionUsecase _usecase;

        public StockTransactionController(StockTransactionUsecase usecase)
        {
            _usecase = usecase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _usecase.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(StockTransaction transaction)
        {
            var result = await _usecase.CreateAsync(transaction);
            return Ok(result);
        }
    }
}
