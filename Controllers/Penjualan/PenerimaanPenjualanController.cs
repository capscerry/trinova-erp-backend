using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class PenerimaanPenjualanController : ControllerBase
    {
        private readonly IPenerimaanPenjualanUsecase _penerimaanUsecase;
        public PenerimaanPenjualanController(IPenerimaanPenjualanUsecase penerimaanUsecase)
        {
            _penerimaanUsecase = penerimaanUsecase;
        }

        [HttpGet("/api/bank")]
        public async Task<IActionResult> GetBankAsync()
        {
            var result = await _penerimaanUsecase.GetBankAsync();
            if (result == null || !result.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "Data Sales Order tidak ditemukan untuk customer ini",
                    data = Array.Empty<object>()
                });
            }
            return Ok(new
            {
                success = true,
                message = "Data Sales Order Berhasil diambil",
                data = result
            });

        }

        [HttpGet("/api/sales-receipt")]
        public async Task<IActionResult> GetAllSalesReceipt()
        {
            try
            {
                var result = await _penerimaanUsecase.GetAllSalesReceipt();

                return Ok(new
                {
                    success = true,
                    message = "Data penerimaan penjualan berhasil diambil.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("/api/sales-receipt")]
        public async Task<IActionResult> InsertSalesReceipt([FromBody] PenerimaanPenjualan dto)
        {
            try
            {
                var result = await _penerimaanUsecase.InsertSalesReceipt(dto);

                return Ok(new
                {
                    success = true,
                    message = "Data penerimaan penjualan berhasil disimpan.",
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
