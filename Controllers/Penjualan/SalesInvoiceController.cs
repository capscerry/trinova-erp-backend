using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class SalesInvoiceController : ControllerBase
    {
        private readonly ISalesInvoiceUsecase _salesInvoiceUsecase;

        public SalesInvoiceController(ISalesInvoiceUsecase salesInvoiceUsecase)
        {
            _salesInvoiceUsecase = salesInvoiceUsecase;
        }

        [HttpGet("/api/sales-invoice")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _salesInvoiceUsecase.GetAll();
                return Ok(new
                {
                    success = true,
                    message = "Data faktur penjualan berhasil diambil.",
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

        [HttpGet("/api/sales-invoice/outstanding")]
        public async Task<IActionResult> GetOutstanding()
        {
            try
            {
                var result = await _salesInvoiceUsecase.GetOutstanding();
                return Ok(new
                {
                    success = true,
                    message = "Data outstanding faktur penjualan berhasil diambil.",
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

        [HttpGet("/api/sales-invoice/customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            try
            {
                var result = await _salesInvoiceUsecase.GetByCustomerId(customerId);
                return Ok(new
                {
                    success = true,
                    message = "Data faktur penjualan customer berhasil diambil.",
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

        [HttpGet("/api/sales-invoice/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _salesInvoiceUsecase.GetById(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Faktur penjualan tidak ditemukan."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Detail faktur penjualan berhasil diambil.",
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

        [HttpPost("/api/sales-invoice")]
        public async Task<IActionResult> Create([FromBody] SalesInvoice model)
        {
            try
            {
                var result = await _salesInvoiceUsecase.Create(model);
                return Ok(new
                {
                    success = true,
                    message = "Faktur penjualan berhasil disimpan.",
                    data = result.Header
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
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("/api/sales-invoice/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SalesInvoice model)
        {
            try
            {
                var result = await _salesInvoiceUsecase.Update(id, model);
                return Ok(new
                {
                    success = true,
                    message = "Faktur penjualan berhasil diperbarui.",
                    data = result.Header
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

        [HttpDelete("/api/sales-invoice/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _salesInvoiceUsecase.Delete(id);
                return Ok(new
                {
                    success = true,
                    message = "Faktur penjualan berhasil dihapus."
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

        [HttpPost("/api/sales-invoice/{id}/send-email")]
        public async Task<IActionResult> SendInvoiceEmail(
            int id,
            [FromBody] SendQuotationEmailRequest? request)
        {
            try
            {
                await _salesInvoiceUsecase.SendInvoiceEmailAsync(id, request);

                return Ok(new
                {
                    success = true,
                    message = "Email faktur (PDF) berhasil dikirim ke pelanggan."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Terjadi kesalahan tak terduga: " + ex.Message });
            }
        }

        [HttpPatch("/api/sales-invoice/{id}/confirm")]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                await _salesInvoiceUsecase.Confirm(id);
                return Ok(new
                {
                    success = true,
                    message = "Faktur penjualan berhasil dikonfirmasi."
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
