using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryStockController : ControllerBase
    {
        private readonly InventoryStockUsecase _usecase;

        public InventoryStockController(InventoryStockUsecase usecase)
        {
            _usecase = usecase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
           return Ok(await _usecase.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Create(InventoryStock stock)
        {
           var result = await _usecase.CreateAsync(stock);
           return Ok(result);
        }
    }
}