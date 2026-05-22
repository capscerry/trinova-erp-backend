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

        [HttpPost]
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
    }
}