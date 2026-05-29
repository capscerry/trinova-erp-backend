using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Controllers.Persediaan
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterWarehouseController : ControllerBase
    {
        private readonly IMasterWarehouseUsecase _masterWarehouseUsecase;

        public MasterWarehouseController(
            IMasterWarehouseUsecase masterWarehouseUsecase
        )
        {
            _masterWarehouseUsecase = masterWarehouseUsecase;
        }

        [HttpPost("/api/master-warehouse")]
        public async Task<IActionResult> InsertMasterWarehouse(
            [FromBody] MasterWarehouse masterWarehouse
        )
        {
            var result = await _masterWarehouseUsecase
                .InsertMasterWarehouse(masterWarehouse);

            return Ok(new
            {
                status = true,
                message = result
            });
        }

        [HttpGet("/api/master-warehouse")]
        public async Task<IActionResult> GetAllMasterWarehouse()
        {
            var result = await _masterWarehouseUsecase
                .GetAllMasterWarehouse();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Warehouse Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/master-warehouse")]
        public async Task<IActionResult> UpdateMasterWarehouse(
            [FromBody] MasterWarehouse masterWarehouse
        )
        {
            var result = await _masterWarehouseUsecase
                .UpdateMasterWarehouse(masterWarehouse);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Update Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Update Data"
            });
        }

        [HttpDelete("/api/master-warehouse/{id}")]
        public async Task<IActionResult> DeleteMasterWarehouse(int id)
        {
            var result = await _masterWarehouseUsecase
                .DeleteMasterWarehouse(id);

            if (result)
            {
                return Ok(new
                {
                    status = true,
                    message = "Success Delete Data"
                });
            }

            return BadRequest(new
            {
                status = false,
                message = "Failed Delete Data"
            });
        }
    }
}