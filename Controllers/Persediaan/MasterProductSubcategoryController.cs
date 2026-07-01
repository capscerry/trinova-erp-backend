using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [ApiController]
    [Route("api/product-subcategories")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class MasterProductSubcategoryController : ControllerBase
    {
        private readonly MasterProductSubcategoryUsecase _usecase;

        public MasterProductSubcategoryController(
            MasterProductSubcategoryUsecase usecase
        )
        {
            _usecase = usecase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _usecase.GetAllAsync();

            return Ok(data);
        }

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var data = await _usecase.GetByCategoryAsync(categoryId);

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] ProductSubcategory model
        )
        {
            try
            {
                var result =
                    await _usecase.CreateAsync(
                        model
                    );

                return Ok(new
                {
                    message =
                        "Subcategory created",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] ProductSubcategory model
        )
        {
            var result = await _usecase.UpdateAsync(id, model);

            if (result == null)
            {
                return NotFound(new
                {
                    message = "Subcategory not found"
                });
            }

            return Ok(new
            {
                message = "Subcategory updated",
                data = result
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _usecase.DeleteAsync(id);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Subcategory not found"
                });
            }

            return Ok(new
            {
                message = "Subcategory deleted"
            });
        }
    }
}
