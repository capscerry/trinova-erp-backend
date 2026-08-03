using Microsoft.AspNetCore.Mvc;

using ExcelDataReader;

using System.Data;

using trinova_erp_backend.Models;

using trinova_erp_backend.Security;
using trinova_erp_backend.Services;

using trinova_erp_backend.Usecase.Pembelian;

namespace trinova_erp_backend.Controllers.Pembelian
{
    public class RestoreStockRequest
    {
        public int product_id  { get; set; }
        public int supplier_id { get; set; }
        public int quantity    { get; set; }
    }

    public class UpdateSupplierProductRequest
    {
        public decimal supplier_price  { get; set; }
        public int      available_stock { get; set; }
        public int      lead_time_days  { get; set; }
        public bool     is_available    { get; set; } = true;
    }

    [Route("api/[controller]")]
    [ApiController]

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Purchasing,purchasing,Pembelian,pembelian,Procurement Manager")]
    public class SupplierProductController
        : ControllerBase
    {
        private readonly
            ISupplierProductUsecase
                _supplierProductUsecase;

        private readonly IConfiguration _configuration;
        private readonly IActivityLogService _activityLogService;

        public SupplierProductController(
            ISupplierProductUsecase
                supplierProductUsecase,
            IConfiguration configuration,
            IActivityLogService activityLogService
        )
        {
            _supplierProductUsecase =
                supplierProductUsecase;
            _configuration = configuration;
            _activityLogService = activityLogService;
        }

        // ─── RESTORE STOCK ─────────────────────

        [HttpPost("/api/supplier-product/restore-stock")]
        public async Task<IActionResult> RestoreStock(
            [FromBody] RestoreStockRequest request
        )
        {
            try
            {
                var result = await _supplierProductUsecase
                    .RestoreStock(
                        request.product_id,
                        request.supplier_id,
                        request.quantity
                    );

                if (!result)
                    return BadRequest(new
                    {
                        status = false,
                        message = $"Product ID {request.product_id} not found for supplier {request.supplier_id}"
                    });

                return Ok(new
                {
                    status = true,
                    message = "Stock restored successfully"
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

        // ─── GET BY SUPPLIER ────────────────────

        [HttpGet("/api/supplier-product/by-supplier/{supplierId}")]

        public async Task<IActionResult>
            GetProductsBySupplier(int supplierId)
        {
            var result =
                await
                    _supplierProductUsecase
                        .GetProductsBySupplier(supplierId);

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
                var maxBytes =
                    _configuration.GetValue<long>(
                        "FileUploadSecurity:MaxExcelSizeBytes",
                        10 * 1024 * 1024
                    );

                var validation =
                    FileUploadSecurity.ValidateExcel(
                        file,
                        maxBytes
                    );

                if (!validation.IsValid)
                {
                    await _activityLogService.LogAsync(
                        new ActivityLogCreate
                        {
                            Module = "security",
                            ActivityType = "file_upload_rejected",
                            Title = $"Supplier catalog upload rejected: {validation.ErrorMessage}",
                            Description =
                                $"Supplier={supplierId}, FileName={file?.FileName}, " +
                                $"Size={file?.Length}, ContentType={file?.ContentType}",
                            RefTable = "master_supplier",
                            RefId = supplierId
                        }
                    );

                    return StatusCode(
                        validation.StatusCode,
                        new
                        {
                            status = false,
                            message = validation.ErrorMessage
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

                    // The header may appear as "available_stock" or
                    // "available_" (truncated in some Excel exports).
                    string availableColName =
                        table.Columns.Contains("available_stock")
                            ? "available_stock"
                            : table.Columns.Contains("available_")
                                ? "available_"
                                : throw new Exception(
                                    "Column 'available_stock' not found in the uploaded file. " +
                                    "Please ensure the header row contains: " +
                                    "product_id, supplier_price, available_stock, lead_time_days"
                                );

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
                                        row[availableColName]
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

        // ─── UPDATE (fix a duplicate/incorrect catalog row) ──────────────

        [HttpPut("/api/supplier-product/{id}")]
        public async Task<IActionResult> UpdateSupplierProduct(
            int id,
            [FromBody] UpdateSupplierProductRequest request
        )
        {
            try
            {
                var result = await _supplierProductUsecase.UpdateSupplierProduct(
                    id,
                    request.supplier_price,
                    request.available_stock,
                    request.lead_time_days,
                    request.is_available
                );

                if (!result)
                    return NotFound(new
                    {
                        status = false,
                        message = $"Supplier product {id} not found"
                    });

                return Ok(new
                {
                    status = true,
                    message = "Catalog entry updated successfully"
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

        // ─── DELETE (remove a stray duplicate catalog row) ────────────────

        [HttpDelete("/api/supplier-product/{id}")]
        public async Task<IActionResult> DeleteSupplierProduct(int id)
        {
            try
            {
                var result = await _supplierProductUsecase.DeleteSupplierProduct(id);

                if (!result)
                    return NotFound(new
                    {
                        status = false,
                        message = $"Supplier product {id} not found"
                    });

                return Ok(new
                {
                    status = true,
                    message = "Catalog entry deleted successfully"
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
    }
}
