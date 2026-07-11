using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class SalesReturnController : ControllerBase
    {
        private readonly ISalesReturnUsecase _salesReturnUsecase;

        public SalesReturnController(ISalesReturnUsecase salesReturnUsecase)
        {
            _salesReturnUsecase = salesReturnUsecase;
        }

        [HttpGet("/api/sales-return")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _salesReturnUsecase.GetAllAsync();
            return Ok(new
            {
                success = true,
                message = "Data Sales Return berhasil diambil",
                data = result
            });
        }

        [HttpGet("/api/sales-return/{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _salesReturnUsecase.GetDetailAsync(id);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Sales return tidak ditemukan"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Detail Sales Return berhasil diambil",
                data = result
            });
        }

        [HttpGet("/api/sales-return/returnable/{deliveryOrderId}")]
        public async Task<IActionResult> GetReturnableQty(int deliveryOrderId)
        {
            try
            {
                var result = await _salesReturnUsecase.GetReturnableQtyAsync(deliveryOrderId);
                return Ok(new
                {
                    success = true,
                    message = "Qty retur tersedia berhasil diambil",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("/api/sales-return")]
        public async Task<IActionResult> Create([FromBody] SalesReturn model)
        {
            try
            {
                var id = await _salesReturnUsecase.InsertSalesReturn(model);

                return Ok(new
                {
                    success = true,
                    message = "Retur penjualan berhasil disimpan, stok telah dikembalikan",
                    data = new { id }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
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
