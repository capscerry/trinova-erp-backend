using Microsoft.AspNetCore.Mvc;

using ExcelDataReader;

using System.Data;

using trinova_erp_backend.Models;

using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [Route("api/[controller]")]
    [ApiController]

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian")]
    public class SupplierProductController
        : ControllerBase
    {
        private readonly
            ISupplierProductUsecase
                _supplierProductUsecase;

        public SupplierProductController(
            ISupplierProductUsecase
                supplierProductUsecase
        )
        {
            _supplierProductUsecase =
                supplierProductUsecase;
        }

        // ─── GET ALL ────────────────────────────

        [HttpGet("/api/supplier-product")]

        public async Task<IActionResult>
            GetAllSupplierProduct()
        {
            var result =
                await
                    _supplierProductUsecase
                        .GetAllSupplierProduct();

            return Ok(new
            {
                status = true,
                data = result
            });
        }

        // ─── IMPORT EXCEL ──────────────────────

        [HttpPost(
            "/api/supplier-product/import/{supplierId}"
        )]

        public async Task<IActionResult>
            ImportSupplierCatalog(
                int supplierId,

                IFormFile file
            )
        {
            try
            {
                if (
                    file == null
                    || file.Length == 0
                )
                {
                    return BadRequest(
                        new
                        {
                            status = false,
                            message =
                                "File not found"
                        }
                    );
                }

                System.Text.Encoding.RegisterProvider(
                    System.Text.CodePagesEncodingProvider.Instance
                );

                var models =
                    new List<SupplierProductImport>();

                using (
                    var stream =
                        file.OpenReadStream()
                )

                using (
                    var reader =
                        ExcelReaderFactory.CreateReader(
                            stream
                        )
                )
                {
                    var result =
                        reader.AsDataSet(
                            new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable =
                                    (_) =>
                                        new ExcelDataTableConfiguration()
                                        {
                                            UseHeaderRow = true
                                        }
                            }
                        );

                    DataTable table =
                        result.Tables[0];

                    foreach (
                        DataRow row
                        in table.Rows
                    )
                    {
                        models.Add(
                            new SupplierProductImport
                            {
                                product_id =
                                    Convert.ToInt32(
                                        row["product_id"]
                                    ),

                                supplier_price =
                                    Convert.ToDecimal(
                                        row["supplier_price"]
                                    ),

                                available_stock =
                                    Convert.ToInt32(
                                        row["available_stock"]
                                    ),

                                lead_time_days =
                                    Convert.ToInt32(
                                        row["lead_time_days"]
                                    )
                            }
                        );
                    }
                }

                if (models.Count == 0)
                {
                    return BadRequest(
                        new
                        {
                            status = false,
                            message =
                                "Excel data empty"
                        }
                    );
                }

                var insertResult =
                    await
                        _supplierProductUsecase
                            .BulkInsertSupplierProduct(
                                supplierId,
                                models
                            );

                if (!insertResult)
                {
                    return BadRequest(
                        new
                        {
                            status = false,
                            message =
                                "Failed insert to database"
                        }
                    );
                }

                return Ok(
                    new
                    {
                        status = true,
                        message =
                            "Import success"
                    }
                );
            }

            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        status = false,
                        message =
                            ex.Message
                    }
                );
            }
        }
    }
}
