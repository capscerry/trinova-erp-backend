using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterProductController : ControllerBase
    {
        private readonly IMasterProductUsecase _masterProductUsecase;

        public MasterProductController(
            IMasterProductUsecase masterProductUsecase
        )
        {
            _masterProductUsecase = masterProductUsecase;
        }

        [HttpPost("/api/master-product")]
        public async Task<IActionResult> InsertMasterProduct(
            [FromBody] MasterProduct masterProduct
        )
        {
            var result = await _masterProductUsecase
                .InsertMasterProduct(masterProduct);

            return Ok(new
            {
                status = true,
                message = result
            });
        }

        [HttpGet("/api/master-product")]
        public async Task<IActionResult> GetAllMasterProduct()
        {
            var result = await _masterProductUsecase
                .GetAllMasterProduct();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Product Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpGet("/api/master-product/{id}")]
        public async Task<IActionResult> GetMasterProductById(
            int id
        )
        {
            var result = await _masterProductUsecase
                .GetMasterProductById(id);

            if (result == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Product Not Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/master-product")]
        public async Task<IActionResult> UpdateMasterProduct(
            [FromBody] MasterProduct masterProduct
        )
        {
            var result = await _masterProductUsecase
                .UpdateMasterProduct(masterProduct);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Product"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Product"
            });
        }

        [HttpDelete("/api/master-product/{id}")]
        public async Task<IActionResult> DeleteMasterProduct(
            int id
        )
        {
            var result = await _masterProductUsecase
                .DeleteMasterProduct(id);

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