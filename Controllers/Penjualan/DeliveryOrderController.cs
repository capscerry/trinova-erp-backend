using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
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

        // All DO detail lines across every delivery order -- used by the Sales
        // dashboard to compute a qty-based Fulfillment Rate. Must be registered
        // before the {deliveryOrderId} route below or routing would try to
        // parse "all" as an int and 404.
        [HttpGet("/api/do-detail/all")]
        public async Task<IActionResult> GetAllDoDetail()
        {
            try
            {
                var result = await _pengirimanUsecase.GetAllDoDetails();
                return Ok(new
                {
                    status = true,
                    message = "Success Fetch Data",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/api/do-detail/{deliveryOrderId}")]
        public async Task<IActionResult> GetDoDetail(int deliveryOrderId)
        {
            try
            {
                var result = await _pengirimanUsecase.GetDoDetail(deliveryOrderId);
                return Ok(new
                {
                    status = true,
                    message = "Success Fetch Data",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("/api/delivery-order")]
        public async Task<IActionResult> InsertDeliveryOrder([FromBody] PengirimanPenjualan model)
        {
            try
            {
                await _pengirimanUsecase.InsertDeliveryOrder(model);
                return Ok(new
                {
                    status = true,
                    message = "Success Insert Data"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPatch("/api/delivery-order/{id}/mark-received")]
        public async Task<IActionResult> MarkDeliveryOrderReceived(int id)
        {
            try
            {
                await _pengirimanUsecase.MarkDeliveryOrderReceivedAsync(id);
                return Ok(new
                {
                    status = true,
                    message = "Delivery Order ditandai diterima. Sales Order terkait telah diselesaikan."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPut("/api/delivery-order/{id}")]
        public async Task<IActionResult> UpdateDeliveryOrder(int id, [FromBody] PengirimanPenjualan model)        {
            try
            {
                await _pengirimanUsecase.UpdateDeliveryOrder(id, model);
                return Ok(new
                {
                    status = true,
                    message = "Success Update Data"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

    }
}
