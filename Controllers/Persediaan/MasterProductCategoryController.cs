using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
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