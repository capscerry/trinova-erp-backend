using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
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

            string query = @"
                SELECT *
                FROM purchase_requisition
                WHERE pr_id = @Id";

            return await connection.QueryFirstOrDefaultAsync<PurchaseRequisition>(
                query,
                new { Id = id }
            );
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

            string today = DateTime.Now.ToString("yyyyMMdd");

            string query = @"
                SELECT COUNT(*)
                FROM purchase_requisition
                WHERE CAST(created_at AS DATE) = CAST(GETDATE() AS DATE)";

            int countToday = await connection.ExecuteScalarAsync<int>(query);

            return $"PR-{today}-{(countToday + 1):D4}";
        }
    }
}