using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan,Sales,Penjualan,sales,penjualan")]
    public class InventoryStockController : ControllerBase
    {
        private readonly InventoryStockUsecase _usecase;

        public InventoryStockController(InventoryStockUsecase usecase)
        {
            _usecase = usecase;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _usecase.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _usecase.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { status = false, message = "Stock record not found" });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(InventoryStock stock)
        {
            var result = await _usecase.CreateAsync(stock);
            return Ok(new { status = true, message = "Stock record created", data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, InventoryStock stock)
        {
            stock.stock_id = id;
            await _usecase.UpdateAsync(stock);
            return Ok(new { status = true, message = "Stock record updated" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _usecase.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { status = false, message = "Stock record not found" });

            await _usecase.DeleteAsync(existing);
            return Ok(new { status = true, message = "Stock record deleted" });
        }
    }
}
