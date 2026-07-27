using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
    public class MasterProductCategoryController : ControllerBase
    {
        private readonly IMasterProductCategoryUsecase _masterProductCategoryUsecase;

        public MasterProductCategoryController(
            IMasterProductCategoryUsecase masterProductCategoryUsecase
        )
        {
            _masterProductCategoryUsecase = masterProductCategoryUsecase;
        }

        [HttpPost("/api/master-product-category")]
        public async Task<IActionResult> InsertMasterProductCategory(
            [FromBody] MasterProductCategory masterProductCategory
        )
        {
            var result = await _masterProductCategoryUsecase
                .InsertMasterProductCategory(masterProductCategory);

            return Ok(new
            {
                status = true,
                message = result
            });
        }

        [HttpGet("/api/master-product-category")]
        // Performance: Product category list is reference data used in dropdowns
        // across the product management UI. Caching for 120 s reduces DB load on
        // repeated form loads. Vary on Authorization so different auth contexts
        // do not share cache entries. Route, response shape, and auth unchanged.
        [ResponseCache(Duration = 120, VaryByHeader = "Authorization", Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetAllMasterProductCategory()
        {
            var result = await _masterProductCategoryUsecase
                .GetAllMasterProductCategory();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Product Category Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/master-product-category")]
        public async Task<IActionResult> UpdateMasterProductCategory(
            [FromBody] MasterProductCategory masterProductCategory
        )
        {
            var result = await _masterProductCategoryUsecase
                .UpdateMasterProductCategory(masterProductCategory);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Data"
            });
        }

        [HttpDelete("/api/master-product-category/{id}")]
        public async Task<IActionResult> DeleteMasterProductCategory(int id)
        {
            var result = await _masterProductCategoryUsecase
                .DeleteMasterProductCategory(id);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Delete Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Delete Data"
            });
        }
    }
}
