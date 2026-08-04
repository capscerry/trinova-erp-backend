using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    /// <summary>
    /// Provides the aggregated stock-usage dataset consumed by the Inventory AI
    /// service (trinova-ai on Railway) to train its demand-forecast model.
    ///
    /// The query aggregates all OUT-type movements from stock_transaction grouped
    /// by product and calendar month — identical to the data the Python service
    /// previously read directly from Azure SQL via pyodbc.
    ///
    /// OUT-type transaction_type values included:
    ///   OUT, TRANSFER_OUT
    ///
    /// Sorting: (product_id ASC, tahun ASC, bulan ASC) — matches the ordering
    /// applied by client/inventory_client.py after loading the DataFrame.
    /// </summary>
    public class ForecastDatasetRepo
    {
        private readonly string _connectionString;

        public ForecastDatasetRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured.");
        }

        public async Task<List<ForecastDatasetItem>> GetForecastDatasetAsync()
        {
            const string sql = @"
                SELECT
                    st.product_id                            AS ProductId,
                    ISNULL(mp.product_name, '')             AS ProductName,
                    YEAR(st.created_at)                     AS Tahun,
                    MONTH(st.created_at)                    AS Bulan,
                    CAST(SUM(st.quantity) AS FLOAT)         AS TotalUsage
                FROM stock_transaction st
                LEFT JOIN master_product mp
                    ON mp.product_id = st.product_id
                WHERE st.transaction_type IN ('OUT', 'TRANSFER_OUT')
                GROUP BY
                    st.product_id,
                    mp.product_name,
                    YEAR(st.created_at),
                    MONTH(st.created_at)
                ORDER BY
                    st.product_id ASC,
                    YEAR(st.created_at) ASC,
                    MONTH(st.created_at) ASC";

            await using var connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<ForecastDatasetItem>(sql);
            return rows.ToList();
        }
    }
}
