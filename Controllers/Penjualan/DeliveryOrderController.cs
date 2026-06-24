using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryOrderController : ControllerBase
    {
        private readonly IPengirimanPenjualanUsecase _pengirimanUsecase;
        public DeliveryOrderController(IPengirimanPenjualanUsecase pengirimanUsecase)
        {
            _pengirimanUsecase = pengirimanUsecase;
        }
        [HttpGet("/api/shipping-type")]
        public async Task<IActionResult> GetShippingCategory()
        {
            var result = await _pengirimanUsecase.GetShippingCategory();
            return Ok(new { 
                 status = true,
                 message = "Success Fetch Data",
                 data = result
            });

        }

        [HttpGet("/api/do-header")]
        public async Task<IActionResult> GetDoHeader()
        {
            var result = await _pengirimanUsecase.GetDoHeader();
            return Ok(new
            {
                status = true,
                message = "Success Fetch Data",
                data = result
            });
        }
    }
}
