using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Controllers.Penjualan
{
    [Route("api/sales-status")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,admin,Sales,sales,Penjualan,penjualan")]
    public class SalesStatusController : ControllerBase
    {
        private readonly string _connectionString;

        private static readonly Dictionary<string, SalesStatusTarget> Targets = new(StringComparer.OrdinalIgnoreCase)
        {
            ["quotation"] = new("sales_quotation", "quotation_id", "status", "update_date"),
            ["sales-order"] = new("sales_order", "order_id", "status", "updated_at"),
            ["down-payment"] = new("uang_muka", "id", "status", "updated_at"),
            ["delivery-order"] = new("delivery_order_header", "id", "status", "updated_at"),
            ["sales-invoice"] = new("sales_invoice", "id", "status", "updated_at"),
            ["sales-receipt"] = new("sales_receipt", "id", "status", "updated_at")
        };

        public SalesStatusController(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        [HttpPatch("{module}/{id:int}")]
        public async Task<IActionResult> UpdateStatus(string module, int id, [FromBody] SalesStatusRequest request)
        {
            if (!Targets.TryGetValue(module, out var target))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Unsupported sales module."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Status is required."
                });
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await EnsureStatusColumns(connection, target);

            var affectedRows = await connection.ExecuteAsync($@"
                UPDATE {target.TableName}
                SET {target.StatusColumn} = @Status,
                    {target.UpdatedAtColumn} = DATEADD(HOUR, 7, GETUTCDATE())
                WHERE {target.IdColumn} = @Id",
                new
                {
                    Id = id,
                    Status = request.Status.Trim()
                });

            if (affectedRows == 0)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Sales document not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Status updated successfully.",
                data = new
                {
                    module,
                    id,
                    status = request.Status.Trim()
                }
            });
        }

        private static async Task EnsureStatusColumns(SqlConnection connection, SalesStatusTarget target)
        {
            var statusColumnExists = await connection.ExecuteScalarAsync<int>($@"
                SELECT CASE WHEN COL_LENGTH('{target.TableName}', '{target.StatusColumn}') IS NULL THEN 0 ELSE 1 END");

            if (statusColumnExists == 0)
            {
                await connection.ExecuteAsync($@"
                    ALTER TABLE {target.TableName}
                    ADD {target.StatusColumn} VARCHAR(30) NOT NULL
                    CONSTRAINT DF_{target.TableName}_{target.StatusColumn} DEFAULT 'Draft'");
            }

            var updatedAtColumnExists = await connection.ExecuteScalarAsync<int>($@"
                SELECT CASE WHEN COL_LENGTH('{target.TableName}', '{target.UpdatedAtColumn}') IS NULL THEN 0 ELSE 1 END");

            if (updatedAtColumnExists == 0)
            {
                await connection.ExecuteAsync($@"
                    ALTER TABLE {target.TableName}
                    ADD {target.UpdatedAtColumn} DATETIME NULL");
            }
        }

        private sealed record SalesStatusTarget(
            string TableName,
            string IdColumn,
            string StatusColumn,
            string UpdatedAtColumn
        );
    }
}
