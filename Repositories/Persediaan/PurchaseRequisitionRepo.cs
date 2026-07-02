using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    // Flat projection used only inside GetByIdAsync to avoid column-name collision
    internal class PrDetailFlat
    {
        public int pr_detail_id { get; set; }
        public int pr_id { get; set; }
        public int product_id { get; set; }
        public decimal qty_requested { get; set; }
        public decimal qty_processed { get; set; }
        public string? remarks { get; set; }
        // product columns (prefixed to avoid collision)
        public string? product_name { get; set; }
        public string? product_code { get; set; }
        public int uom_id { get; set; }
    }

    public class PurchaseRequisitionRepo
    {
        private readonly string _connectionString;

        public PurchaseRequisitionRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public async Task<List<PurchaseRequisition>> GetAllAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                SELECT *
                FROM purchase_requisition
                ORDER BY created_at DESC";

            var result = await connection.QueryAsync<PurchaseRequisition>(query);

            return result.ToList();
        }

        public async Task<PurchaseRequisition?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            // Load header
            var pr = await connection.QueryFirstOrDefaultAsync<PurchaseRequisition>(
                @"SELECT * FROM purchase_requisition WHERE pr_id = @Id",
                new { Id = id }
            );

            if (pr == null) return null;

            // Load details as a flat projection — avoids Dapper splitOn ambiguity
            // when both prd and mp share a product_id column name.
            string detailQuery = @"
                SELECT
                    prd.pr_detail_id,
                    prd.pr_id,
                    prd.product_id,
                    prd.qty_requested,
                    prd.qty_processed,
                    prd.remarks,
                    mp.product_name,
                    mp.product_code,
                    mp.uom_id
                FROM purchase_requisition_detail prd
                INNER JOIN master_product mp ON mp.product_id = prd.product_id
                WHERE prd.pr_id = @Id";

            var flats = await connection.QueryAsync<PrDetailFlat>(
                detailQuery,
                new { Id = id }
            );

            pr.Details = flats.Select(f => new PurchaseRequisitionDetail
            {
                pr_detail_id  = f.pr_detail_id,
                pr_id         = f.pr_id,
                product_id    = f.product_id,
                qty_requested = f.qty_requested,
                qty_processed = f.qty_processed,
                remarks       = f.remarks,
                Product = new MasterProduct
                {
                    product_id   = f.product_id,
                    product_name = f.product_name,
                    product_code = f.product_code,
                    uom_id       = f.uom_id,
                },
            }).ToList();

            return pr;
        }

        public async Task<PurchaseRequisition> CreateAsync(PurchaseRequisition requisition)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                INSERT INTO purchase_requisition
                (
                    pr_number,
                    warehouse_id,
                    notes,
                    status,
                    created_at
                )
                OUTPUT INSERTED.*
                VALUES
                (
                    @pr_number,
                    @warehouse_id,
                    @notes,
                    @status,
                    GETDATE()
                )";

            var result = await connection.QuerySingleAsync<PurchaseRequisition>(
                query,
                requisition
            );

            return result;
        }

        public async Task UpdateAsync(PurchaseRequisition requisition)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                UPDATE purchase_requisition
                SET
                    warehouse_id = @warehouse_id,
                    notes = @notes,
                    status = @status
                WHERE pr_id = @pr_id";

            await connection.ExecuteAsync(query, requisition);
        }

        public async Task DeleteAsync(PurchaseRequisition requisition)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                DELETE FROM purchase_requisition
                WHERE pr_id = @Id";

            await connection.ExecuteAsync(query, new
            {
                Id = requisition.pr_id
            });
        }

        public async Task<string> GeneratePrNumber()
        {
            using var connection = new SqlConnection(_connectionString);

            const string query = @"
                SELECT TOP 1 pr_number
                FROM purchase_requisition
                ORDER BY pr_id DESC";

            await connection.OpenAsync();

            object? result = await connection
                .ExecuteScalarAsync<string>(query);

            int nextNumber = 1;

            if (result != null)
            {
                string lastPr =
                    result.ToString() ?? "PR000000";

                string numericPart =
                    lastPr.Replace("PR", "");

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"PR{nextNumber:D6}";
        }
    }
}
