using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierUsecase _supplierUsecase;

        public SupplierController(ISupplierUsecase supplierUsecase)
        {
            _supplierUsecase = supplierUsecase;
        }

        [HttpPost("/api/supplier")]
        public async Task<IActionResult> InsertSupplier([FromBody] Supplier supplier)
        {
            var result = await _supplierUsecase.InsertSupplier(supplier);

            return Ok(new
            {
                status = true,
                message = result
            });
        }

        [HttpGet("/api/supplier")]
        public async Task<IActionResult> GetAllSupplier()
        {
            var result = await _supplierUsecase.GetAllSupplier();

            if (result == null || result.Count == 0)
            {
                return Ok(new
                {
                    status = true,
                    data = new List<object>(),
                    message = "No Supplier Found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        [HttpPut("/api/supplier/{id}")]
        public async Task<IActionResult> UpdateSupplier(
            int id,
            [FromBody] Supplier model
        )
        {
            model.supplier_id = id;

            var result = await _supplierUsecase.UpdateSupplier(model);

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

        [HttpDelete("/api/supplier/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var result = await _supplierUsecase.DeleteSupplier(id);

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