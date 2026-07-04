using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
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

                var created = (await _uangMukaUsecase.GetAllUangMuka())
                    .FirstOrDefault(item =>
                        string.Equals(item.NoFaktur, model.NoFaktur, StringComparison.OrdinalIgnoreCase)
                        && item.CustomerId == model.CustomerId);

                return Ok(new
                {
                    success = true,
                    message = "Data uang muka berhasil disimpan",
                    data = created ?? model
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

        [HttpPut("/api/uang-muka/{id}")]
        public async Task<IActionResult> UpdateUangMuka(int id, [FromBody] UangMuka model)
        {
            try
            {
                var result = await _uangMukaUsecase.UpdateUangMuka(id, model);

                if (!result)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Gagal memperbarui data uang muka"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Data uang muka berhasil diperbarui"
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
