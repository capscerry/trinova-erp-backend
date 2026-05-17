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
        private readonly ISalesOrderUsecase _salesOrderUsecase;

        public SalesController(
            ISalesQuotationUsecase salesQuotationUsecase,
            ISalesCategoryUsecase salesCategoryUsecase,
            ISalesOrderUsecase salesOrderUsecase
        )
        {
            _salesQuotationUsecase = salesQuotationUsecase;
            _salesCategoryUsecase = salesCategoryUsecase;
            _salesOrderUsecase = salesOrderUsecase;
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

        // SALES ORDER CONTROLER 

        [HttpPost("/api/sales-order")]
        public async Task<IActionResult> PopulateSalesOrder([FromBody] SalesOrderRequest data)
        {
            try
            {
                if (data == null)
                {
                    return BadRequest(new
                    {
                        message = "Request tidak boleh kosong"
                    });
                }

                if (data.Header == null)
                {
                    return BadRequest(new
                    {
                        message = "Header sales order wajib diisi"
                    });
                }

                if (data.Detail == null || !data.Detail.Any())
                {
                    return BadRequest(new
                    {
                        message = "Detail sales order wajib diisi"
                    });
                }

                var result = await _salesOrderUsecase.InsertSalesOrder(data);

                return Ok(new
                {
                    success = true,
                    message = "Sales Order berhasil dibuat",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


    }
}