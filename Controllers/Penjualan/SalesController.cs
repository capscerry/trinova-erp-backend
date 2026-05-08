using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesController : ControllerBase
    {
        private readonly ISalesQuotationUsecase _salesQuotationUsecase;
        private readonly ISalesCategoryUsecase _salesCategoryUsecase;

        public SalesController(
            ISalesQuotationUsecase salesQuotationUsecase,
            ISalesCategoryUsecase salesCategoryUsecase
        )
        {
            _salesQuotationUsecase = salesQuotationUsecase;
            _salesCategoryUsecase = salesCategoryUsecase;
        }

        [HttpPost("/api/SalesQuotation")]
        public async Task<IActionResult> InsertQuotation(
            [FromBody] SalesQuotation quotation
        )
        {
            try
            {
                if (quotation == null)
                {
                    return BadRequest(new
                    {
                        message = "Quotation payload cannot be null"
                    });
                }

                string result = await _salesQuotationUsecase
                    .InsertSalesQuotation(quotation);

                return Ok(new
                {
                    success = true,
                    message = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("/api/sales-category")]
        public async Task<IActionResult> InsertCategorySales([FromBody] SalesCategory data)
        {
            var result = await _salesCategoryUsecase.InsertDataCategory(data);
            return Ok(result);
        }

        [HttpPost("/api/sales-category/{id}/status")]
        public async Task<IActionResult> UpdateStatusCategory(int id, int status)
        {
            var result = await _salesCategoryUsecase.UpdateStatusCategory(id, status);
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

        [HttpPut("/api/sales-category/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] SalesCategory model)
        {
            model.Id = id;
            var result = await _salesCategoryUsecase.UpdateDataCategory(model);
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

        [HttpGet("/api/sales-category")]
        public async Task<IActionResult> GetAllCategory()
        {
            var result = await _salesCategoryUsecase.GetAllCategory();
            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),  // array kosong
                    message = "No Category Found"
                });
            }
            return Ok(new
            {
                status = true,
                data = result

            });

        }


    }
}