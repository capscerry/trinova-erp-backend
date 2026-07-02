using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan,Purchasing,purchasing,Pembelian,pembelian")]
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
            var result =
                await _masterProductUsecase
                    .InsertMasterProduct(masterProduct);

            if (result == "Insert Failed")
            {
                return BadRequest(new
                {
                    status = false,
                    message = result
                });
            }

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
                    message = "Success Delete Product"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Delete Product"
            });
        }

        [HttpGet("/api/product-data")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductData()
        {
            try
            {
                var result = await _masterProductUsecase.GetAllProduct();
                return Ok(new
                {
                    status = true,
                    message = "Success Fetch Data ",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {   
                    status = false,
                    message = ex.Message,
                    data = (object?)null
                });
            }
        }
    }
}
