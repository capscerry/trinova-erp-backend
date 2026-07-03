using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class PurchaseRequisitionDetailRepo
    {
        private readonly string _connectionString;

        public PurchaseRequisitionDetailRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString =
                options.Value.SQLServer!;
        }

        public async Task CreateAsync(
            PurchaseRequisitionDetail detail
        )
        {
            using var connection =
                new SqlConnection(_connectionString);

            string query = @"
                INSERT INTO purchase_requisition_detail
                (
                    pr_id,
                    product_id,
                    qty_requested,
                    qty_processed,
                    remarks
                )
                VALUES
                (
                    @pr_id,
                    @product_id,
                    @qty_requested,
                    @qty_processed,
                    @remarks
                )";

            await connection.ExecuteAsync(
                query,
                detail
            );
        }
    }
}