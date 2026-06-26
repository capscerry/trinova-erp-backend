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
            
            Console.WriteLine("GET DETAIL REPO CALLED");

            using var connection = new SqlConnection(_connectionString);

            string query = @"
                SELECT
                    pr.*,
                    mw.warehouse_id,
                    mw.warehouse_name,
                    mw.description,
                    mw.warehouse_address,
                    mw.warehouse_type
                FROM purchase_requisition pr
                LEFT JOIN master_warehouse mw
                    ON pr.warehouse_id = mw.warehouse_id
                ORDER BY pr.created_at DESC";

            var result = await connection.QueryAsync<
                PurchaseRequisition,
                MasterWarehouse,
                PurchaseRequisition
            >(
                query,
                (pr, warehouse) =>
                {
                    pr.Warehouse = warehouse;
                    return pr;
                },
                splitOn: "warehouse_id"
            );

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

        public async Task<PurchaseRequisition?> GetDetailAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string prQuery = @"
                SELECT
                    pr.*,
                    mw.warehouse_id,
                    mw.warehouse_name,
                    mw.description,
                    mw.warehouse_address,
                    mw.warehouse_type
                FROM purchase_requisition pr
                LEFT JOIN master_warehouse mw
                    ON pr.warehouse_id = mw.warehouse_id
                WHERE pr.pr_id = @Id";

            var pr = await connection.QueryAsync<
                PurchaseRequisition,
                MasterWarehouse,
                PurchaseRequisition
            >(
                prQuery,
                (requisition, warehouse) =>
                {
                    requisition.Warehouse = warehouse;
                    return requisition;
                },
                new { Id = id },
                splitOn: "warehouse_id"
            );

            var result = pr.FirstOrDefault();

            if (result == null)
                return null;

                string detailQuery = @"
                    SELECT
                        d.*,
                        p.product_name
                    FROM purchase_requisition_detail d
                    LEFT JOIN master_product p
                        ON d.product_id = p.product_id
                    WHERE d.pr_id = @Id";

                var details =
                    await connection.QueryAsync<
                        PurchaseRequisitionDetail
                    >(
                        detailQuery,
                        new { Id = id }
                    );

            result.Details = details.ToList();

            return result;
        }

        public async Task<PurchaseRequisition> CreateAsync(PurchaseRequisition requisition)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                INSERT INTO purchase_requisition
                (
                    pr_number,
                    pr_date,
                    warehouse_id,
                    remarks,
                    status,
                    created_at
                )
                OUTPUT INSERTED.*
                VALUES
                (
                    @pr_number,
                    @pr_date,
                    @warehouse_id,
                    @remarks,
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
                    pr_date = @pr_date,
                    warehouse_id = @warehouse_id,
                    remarks = @remarks,
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