using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;

namespace trinova_erp_backend.Controllers.Pembelian
{
    /// <summary>
    /// One-shot data-fix controller.
    /// DELETE after running — do not ship to production.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DataFixController : ControllerBase
    {
        private readonly string _connectionString;

        public DataFixController(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("DB connection string missing.");
        }

        /// <summary>
        /// Finds every "Cash Refund Credit" payment that is a duplicate of a
        /// same-day, same-amount "Return Credit" payment on the same invoice
        /// (caused by the double-booking bug now fixed in PurchaseReturnUsecase),
        /// and deletes those spurious rows.
        ///
        /// Safe to call multiple times — it only deletes rows that still match
        /// the duplicate pattern.
        ///
        /// GET /api/datafix/preview-duplicate-credits  — show what would be deleted
        /// DELETE /api/datafix/duplicate-credits        — actually delete them
        /// </summary>
        [HttpGet("preview-duplicate-credits")]
        public async Task<IActionResult> PreviewDuplicateCredits()
        {
            var rows = await FindDuplicates();
            return Ok(new
            {
                status  = true,
                count   = rows.Count,
                message = rows.Count == 0
                    ? "No duplicates found — nothing to clean up."
                    : $"{rows.Count} duplicate Cash Refund Credit row(s) would be deleted.",
                duplicates = rows
            });
        }

        [HttpDelete("duplicate-credits")]
        public async Task<IActionResult> DeleteDuplicateCredits()
        {
            var rows = await FindDuplicates();

            if (rows.Count == 0)
                return Ok(new { status = true, deleted = 0, message = "No duplicates found." });

            var ids = string.Join(",", rows.Select(r => r.DuplicateId));

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM purchase_payment WHERE purchase_payment_id IN ({ids})";
            int deleted = await cmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                status  = true,
                deleted,
                message = $"Deleted {deleted} duplicate Cash Refund Credit payment(s).",
                removed = rows
            });
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private record DuplicateRow(
            int     DuplicateId,
            string  DuplicateNumber,
            int     KeeperId,
            string  KeeperNumber,
            int     InvoiceId,
            decimal Amount,
            string  PaymentDate
        );

        private async Task<List<DuplicateRow>> FindDuplicates()
        {
            const string sql = @"
                SELECT
                    crc.purchase_payment_id  AS duplicate_id,
                    crc.payment_number       AS duplicate_number,
                    rc.purchase_payment_id   AS keeper_id,
                    rc.payment_number        AS keeper_number,
                    rc.purchase_invoice_id   AS invoice_id,
                    rc.amount,
                    CONVERT(varchar(10), rc.payment_date, 120) AS payment_date
                FROM purchase_payment rc
                JOIN purchase_payment crc
                    ON  rc.purchase_invoice_id = crc.purchase_invoice_id
                    AND rc.amount              = crc.amount
                    AND CAST(rc.payment_date  AS DATE) = CAST(crc.payment_date AS DATE)
                WHERE rc.payment_method  = 'Return Credit'
                  AND crc.payment_method = 'Cash Refund Credit'
                ORDER BY rc.purchase_invoice_id";

            var results = new List<DuplicateRow>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new DuplicateRow(
                    DuplicateId:     Convert.ToInt32(reader["duplicate_id"]),
                    DuplicateNumber: reader["duplicate_number"]?.ToString() ?? "",
                    KeeperId:        Convert.ToInt32(reader["keeper_id"]),
                    KeeperNumber:    reader["keeper_number"]?.ToString() ?? "",
                    InvoiceId:       Convert.ToInt32(reader["invoice_id"]),
                    Amount:          Convert.ToDecimal(reader["amount"]),
                    PaymentDate:     reader["payment_date"]?.ToString() ?? ""
                ));
            }

            return results;
        }
    }
}
