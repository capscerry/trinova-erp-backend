using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterUomController : ControllerBase
    {
        private readonly IMasterUomUsecase _masterUomUsecase;

        public MasterUomController(
            IMasterUomUsecase masterUomUsecase
        )
        {
            _masterUomUsecase = masterUomUsecase;
        }

        [HttpGet("GetAllMasterUom")]
        public async Task<IActionResult> GetAllMasterUom()
        {
            var result = await _masterUomUsecase.GetAllMasterUom();

            return Ok(new
            {
                status = true,
                data = result
            });
        }
    }
}