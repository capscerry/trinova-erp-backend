using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Inventory,inventory,Warehouse,warehouse,Persediaan,persediaan")]
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
        [AllowAnonymous]
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
