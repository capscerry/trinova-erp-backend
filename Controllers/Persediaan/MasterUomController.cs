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
        // Performance: UOM list is static reference data that rarely changes.
        // Cache the response for 120 seconds to reduce DB round-trips on every
        // form load. NoStore=false so the middleware can serve from cache.
        // VaryByHeader="Accept" ensures content-negotiation stays correct.
        // Business logic, response shape, and route are completely unchanged.
        [ResponseCache(Duration = 120, VaryByHeader = "Accept", Location = ResponseCacheLocation.Any)]
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
