using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesQuotationController : ControllerBase
    {
        private readonly ISalesQuotationUsecase _salesQuotationUsecase;

        public SalesQuotationController(
            ISalesQuotationUsecase salesQuotationUsecase
        )
        {
            _salesQuotationUsecase = salesQuotationUsecase;
        }

        [HttpPost]
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
    }
}