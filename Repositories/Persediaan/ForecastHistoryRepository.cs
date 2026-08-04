using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class ForecastHistoryRepository
    {
        private readonly string _connectionString;

        public ForecastHistoryRepository(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task ReplaceForecast(
            List<ForecastResult> forecasts
        )
        {
            using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var transaction =
                connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(
                    "DELETE FROM forecast_history",
                    transaction: transaction
                );

                const string query = @"
                    INSERT INTO forecast_history
                    (
                        product_id,
                        forecast_month,
                        forecast_quantity,
                        generated_at,
                        historical_records,
                        last_training_period,
                        model_name,
                        created_by
                    )
                    VALUES
                    (
                        @ProductId,
                        @ForecastMonth,
                        @ForecastNextMonth,
                        @GeneratedAt,
                        @HistoricalRecords,
                        @LastTrainingPeriod,
                        @ModelName,
                        @CreatedBy
                    )";

                foreach (var forecast in forecasts)
                {
                    await connection.ExecuteAsync(
                        query,
                        new
                        {
                            ProductId = forecast.ProductId,
                            ForecastMonth = forecast.ForecastMonth,
                            ForecastNextMonth = forecast.ForecastNextMonth,
                            GeneratedAt = forecast.GeneratedAt,
                            HistoricalRecords = forecast.HistoricalRecords,
                            LastTrainingPeriod = forecast.LastTrainingPeriod,
                            ModelName = "Linear Regression",
                            CreatedBy = "SYSTEM"
                        },
                        transaction
                    );
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<ForecastResult>> GetLatestForecast()
        {
            const string query = @"
                SELECT
                    fh.product_id           AS ProductId,
                    mp.product_name         AS ProductName,
                    fh.forecast_month       AS ForecastMonth,
                    fh.forecast_quantity    AS ForecastNextMonth,
                    fh.generated_at         AS GeneratedAt,
                    fh.historical_records   AS HistoricalRecords,
                    fh.last_training_period AS LastTrainingPeriod
                FROM forecast_history fh
                INNER JOIN master_product mp
                    ON fh.product_id = mp.product_id
                ORDER BY mp.product_name;";

            using var connection =
                new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<ForecastResult>(query);

            return result.ToList();
        }
    }
}