using Microsoft.AspNetCore.Mvc;

using trinova_erp_backend.Models;

using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]

    [ApiController]

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
    public class SupplierController
        : ControllerBase
    {
        private readonly ISupplierUsecase
            _supplierUsecase;

        public SupplierController(
            ISupplierUsecase supplierUsecase
        )
        {
            _supplierUsecase =
                supplierUsecase;
        }

        // ─── MIGRATE EXISTING CODES ─────────────

        [HttpPost("/api/supplier/migrate-codes")]

        public async Task<IActionResult>
            MigrateSupplierCodes()
        {
            await _supplierUsecase
                .MigrateSupplierCodes();

            return Ok(new
            {
                status  = true,
                message = "Supplier codes migrated"
            });
        }

        // ─── GET NEXT SUPPLIER CODE ─────────────

        [HttpGet("/api/supplier/next-code")]

        public async Task<IActionResult>
            GetNextSupplierCode()
        {
            var code =
                await _supplierUsecase
                    .GenerateSupplierCode();

            return Ok(new
            {
                supplier_code = code
            });
        }

        // ─── INSERT ─────────────────────────────

        [HttpPost("/api/supplier")]

        public async Task<IActionResult>
            InsertSupplier(
                [FromBody]
                Supplier supplier
            )
        {
            try
            {
                var result =
                    await _supplierUsecase
                        .InsertSupplier(
                            supplier
                        );

                if (result == null)
                {
                    return BadRequest(new
                    {
                        status = false,

                        message =
                            "Insert Failed"
                    });
                }

                return Ok(new
                {
                    status = true,

                    message =
                        "Insert Successfully",

                    data = new
                    {
                        supplier_id =
                            result.supplier_id,

                        supplier_name =
                            result.supplier_name
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // ─── GET ALL ────────────────────────────

        [HttpGet("/api/supplier")]

        public async Task<IActionResult>
            GetAllSupplier()
        {
            var result =
                await _supplierUsecase
                    .GetAllSupplier();

            if (
                result == null
                || result.Count == 0
            )
            {
                return Ok(new
                {
                    status = true,

                    data =
                        new List<object>(),

                    message =
                        "No Supplier Found"
                });
            }

            return Ok(new
            {
                status = true,

                data = result
            });
        }

        // ─── UPDATE ─────────────────────────────

        [HttpPut("/api/supplier/{id}")]

        public async Task<IActionResult>
            UpdateSupplier(
                int id,

                [FromBody]
                Supplier model
            )
        {
            model.supplier_id =
                id;

            var result =
                await _supplierUsecase
                    .UpdateSupplier(model);

            if (result)
            {
                return Ok(new
                {
                    status = true,

                    message =
                        "Success Update Data"
                });
            }

            return BadRequest(new
            {
                status = false,

                message =
                    "Failed Update Data"
            });
        }

        // ─── DELETE ─────────────────────────────

        [HttpDelete("/api/supplier/{id}")]

        public async Task<IActionResult>
            DeleteSupplier(
                int id
            )
        {
            var result =
                await _supplierUsecase
                    .DeleteSupplier(id);

            if (result)
            {
                return Ok(new
                {
                    status = true,

                    message =
                        "Success Delete Data"
                });
            }

            return BadRequest(new
            {
                status = false,

                message =
                    "Failed Delete Data"
            });
        }
    }
}
