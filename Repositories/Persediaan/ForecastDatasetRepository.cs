using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class ForecastDatasetRepository
    {
        private readonly string _connectionString;

        public ForecastDatasetRepository(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<List<ForecastDatasetItem>> GetForecastDataset()
        {
            const string query = @"
                SELECT
                    product_id     AS ProductId,
                    product_name   AS ProductName,
                    tahun          AS Tahun,
                    bulan          AS Bulan,
                    total_usage    AS TotalUsage
                FROM vw_forecast_dataset
                ORDER BY product_id, tahun, bulan";

            using var connection =
                new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<ForecastDatasetItem>(query);

            return result.ToList();
        }
    }
}