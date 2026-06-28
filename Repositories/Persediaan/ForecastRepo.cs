using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class ForecastRepo
    {
        private readonly string _connectionString;

        public ForecastRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<List<ForecastResult>>
            GetForecastData()
        {
            const string query = @"
                SELECT
                    p.product_id,
                    p.product_name,

                    ISNULL(
                        SUM(
                            CASE
                                WHEN st.transaction_type = 'OUT'
                                THEN st.quantity
                                ELSE 0
                            END
                        ),
                        0
                    ) AS total_usage,

                    ISNULL(
                        MAX(i.qty_available),
                        0
                    ) AS current_stock

                FROM master_product p

                LEFT JOIN stock_transaction st
                    ON p.product_id = st.product_id

                LEFT JOIN inventory_stock i
                    ON p.product_id = i.product_id

                GROUP BY
                    p.product_id,
                    p.product_name

                HAVING
                    SUM(
                        CASE
                            WHEN st.transaction_type = 'OUT'
                            THEN st.quantity
                            ELSE 0
                        END
                    ) > 0

                ORDER BY
                    p.product_name;
            ";

            using var connection =
                new SqlConnection(_connectionString);

            var result =
                await connection.QueryAsync<ForecastResult>(
                    query
                );

            return result.ToList();
        }
    }
}