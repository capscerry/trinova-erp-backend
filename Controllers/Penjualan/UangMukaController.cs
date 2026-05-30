using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    public class UangMukaController : ControllerBase
    {
        private readonly IUangMukaUsecase _uangMukaUsecase;

        public UangMukaController(IUangMukaUsecase uangMukaUsecase)
        {
            _uangMukaUsecase = uangMukaUsecase;
        }

        [HttpPost("/api/uang-muka")]
        public async Task<IActionResult> InsertUangMuka([FromBody] UangMuka model)
        {
            try
            {
                var result = await _uangMukaUsecase.InsertUangMuka(model);

                if (!result)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal menyimpan data uang muka"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Data uang muka berhasil disimpan",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/api/uang-muka/{id}")]
        public async Task<IActionResult> GetUangMukaById(int id)
        {
            var result = await _uangMukaUsecase.GetUangMukaById(id);

            if (result == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Data uang muka tidak ditemukan"
                });
            }

            return Ok(new
            {
                status = true,
                message = "Data uang muka berhasil diambil",
                data = result
            });
        }

        [HttpGet("/api/uang-muka")]
        public async Task<IActionResult> GetAllUangMuka()
        {
            try
            {
                var result = await _uangMukaUsecase.GetAllUangMuka();

                return Ok(new
                {
                    success = true,
                    message = "Data uang muka berhasil diambil",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}