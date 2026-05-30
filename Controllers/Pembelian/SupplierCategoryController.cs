using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierCategoryController : ControllerBase
    {
        private readonly ISupplierCategoryUsecase _supplierCategoryUsecase;

        public SupplierCategoryController(
            ISupplierCategoryUsecase supplierCategoryUsecase
        )
        {
            _supplierCategoryUsecase = supplierCategoryUsecase;
        }

        [HttpPost("/api/supplier-category")]
        public async Task<IActionResult> InsertSupplierCategory(
            [FromBody] SupplierCategory category
        )
        {
            var result = await _supplierCategoryUsecase
                .InsertSupplierCategory(category);

            return Ok(new
            {
                status = true,
                message = result
            });
        }

        [HttpGet("/api/supplier-category")]
        public async Task<IActionResult> GetAllSupplierCategory()
        {
            var result = await _supplierCategoryUsecase
                .GetAllSupplierCategory();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Category Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/supplier-category/{id}")]
        public async Task<IActionResult> UpdateSupplierCategory(
            int id,
            [FromBody] SupplierCategory model
        )
        {
            model.category_id = id;

            var result = await _supplierCategoryUsecase
                .UpdateSupplierCategory(model);

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

        [HttpDelete("/api/supplier-category/{id}")]
        public async Task<IActionResult>
            DeleteSupplierCategory(
                int id
            )
        {
            try
            {
                var result =
                    await _supplierCategoryUsecase
                        .DeleteSupplierCategory(id);

                return Ok(new
                {
                    status = true,
                    message = "Success Delete Data"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
    }
}