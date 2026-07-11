using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

//StockTransactionRepo.cs
//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Options;
//using trinova_erp_backend.Config;
//using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class StockTransactionRepo
    {
        private readonly string _connectionString;

        public StockTransactionRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // =========================
        // GET ALL
        // =========================
        public async Task<List<StockTransaction>> GetAllAsync()
        {
            const string query = @"
                SELECT
                    st.*,

                    p.product_name,
                    p.product_code,
                    p.product_type,

                    w.warehouse_name

                FROM stock_transaction st

                LEFT JOIN master_product p
                    ON st.product_id = p.product_id

                LEFT JOIN master_warehouse w
                    ON st.warehouse_id = w.warehouse_id

                ORDER BY st.created_at DESC";

            var response = new List<StockTransaction>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(new StockTransaction
                {
                    transaction_id =
                        Convert.ToInt32(reader["transaction_id"]),

                    product_id =
                        Convert.ToInt32(reader["product_id"]),

                    warehouse_id =
                        Convert.ToInt32(reader["warehouse_id"]),

                    transaction_type =
                        reader["transaction_type"]?.ToString()
                        ?? string.Empty,

                    quantity =
                        Convert.ToDecimal(reader["quantity"]),

                    reference_no =
                        reader["reference_no"] == DBNull.Value
                            ? null
                            : reader["reference_no"].ToString(),

                    reference_module =
                        reader["reference_module"] == DBNull.Value
                            ? null
                            : reader["reference_module"].ToString(),

                    reference_id =
                        reader["reference_id"] == DBNull.Value
                            ? null
                            : Convert.ToInt32(reader["reference_id"]),

                    remarks =
                        reader["remarks"] == DBNull.Value
                            ? null
                            : reader["remarks"].ToString(),

                    created_at =
                        Convert.ToDateTime(reader["created_at"]),

                    Product = new MasterProduct
                    {
                        product_id =
                            Convert.ToInt32(reader["product_id"]),

                        product_name =
                            reader["product_name"]?.ToString(),

                        product_code =
                            reader["product_code"]?.ToString(),

                        product_type =
                            reader["product_type"]?.ToString()
                    },

                    Warehouse = new MasterWarehouse
                    {
                        warehouse_id =
                            Convert.ToInt32(reader["warehouse_id"]),

                        warehouse_name =
                            reader["warehouse_name"]?.ToString()
                    }
                });
            }

            return response;
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<StockTransaction?> GetByIdAsync(int id)
        {
            const string query = @"
                SELECT
                    st.*,

                    p.product_name,
                    p.product_code,
                    p.product_type,

                    w.warehouse_name

                FROM stock_transaction st

                LEFT JOIN master_product p
                    ON st.product_id = p.product_id

                LEFT JOIN master_warehouse w
                    ON st.warehouse_id = w.warehouse_id

                WHERE st.transaction_id = @transaction_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@transaction_id",
                id
            );

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new StockTransaction
            {
                transaction_id =
                    Convert.ToInt32(reader["transaction_id"]),

                product_id =
                    Convert.ToInt32(reader["product_id"]),

                warehouse_id =
                    Convert.ToInt32(reader["warehouse_id"]),

                transaction_type =
                    reader["transaction_type"]?.ToString()
                    ?? string.Empty,

                quantity =
                    Convert.ToDecimal(reader["quantity"]),

                reference_no =
                    reader["reference_no"] == DBNull.Value
                        ? null
                        : reader["reference_no"].ToString(),

                reference_module =
                    reader["reference_module"] == DBNull.Value
                        ? null
                        : reader["reference_module"].ToString(),

                reference_id =
                    reader["reference_id"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["reference_id"]),

                remarks =
                    reader["remarks"] == DBNull.Value
                        ? null
                        : reader["remarks"].ToString(),

                created_at =
                    Convert.ToDateTime(reader["created_at"]),

                Product = new MasterProduct
                {
                    product_id =
                        Convert.ToInt32(reader["product_id"]),

                    product_name =
                        reader["product_name"]?.ToString(),

                    product_code =
                        reader["product_code"]?.ToString(),

                    product_type =
                        reader["product_type"]?.ToString()
                },

                Warehouse = new MasterWarehouse
                {
                    warehouse_id =
                        Convert.ToInt32(reader["warehouse_id"]),

                    warehouse_name =
                        reader["warehouse_name"]?.ToString()
                }
            };
        }

        // =========================
        // CREATE
        // =========================
        public async Task<StockTransaction> CreateAsync(
            StockTransaction transaction
        )
        {
            const string query = @"
                INSERT INTO stock_transaction
                (
                    product_id,
                    warehouse_id,
                    transaction_type,
                    quantity,
                    reference_no,
                    reference_module,
                    reference_id,
                    remarks,
                    created_at
                )
                VALUES
                (
                    @product_id,
                    @warehouse_id,
                    @transaction_type,
                    @quantity,
                    @reference_no,
                    @reference_module,
                    @reference_id,
                    @remarks,
                    @created_at
                )";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@product_id",
                transaction.product_id
            );

            command.Parameters.AddWithValue(
                "@warehouse_id",
                transaction.warehouse_id
            );

            command.Parameters.AddWithValue(
                "@transaction_type",
                transaction.transaction_type
            );

            command.Parameters.AddWithValue(
                "@quantity",
                transaction.quantity
            );

            command.Parameters.AddWithValue(
                "@reference_no",
                (object?)transaction.reference_no ?? DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@reference_module",
                (object?)transaction.reference_module ?? DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@reference_id",
                (object?)transaction.reference_id ?? DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@remarks",
                (object?)transaction.remarks ?? DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@created_at",
                transaction.created_at
            );

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();

            return transaction;
        }

        // =========================
        // CREATE (within an existing transaction)
        // =========================
        // Same insert as CreateAsync, but runs on the caller's connection
        // and transaction so it's committed/rolled back atomically with
        // whatever stock change it's auditing (e.g. SO reserve, DO deduct).
        public async Task CreateAsync(
            StockTransaction transaction,
            SqlConnection connection,
            SqlTransaction sqlTransaction
        )
        {
            const string query = @"
                INSERT INTO stock_transaction
                (
                    product_id,
                    warehouse_id,
                    transaction_type,
                    quantity,
                    reference_no,
                    reference_module,
                    reference_id,
                    remarks,
                    created_at
                )
                VALUES
                (
                    @product_id,
                    @warehouse_id,
                    @transaction_type,
                    @quantity,
                    @reference_no,
                    @reference_module,
                    @reference_id,
                    @remarks,
                    @created_at
                )";

            using var command = new SqlCommand(query, connection, sqlTransaction);

            command.Parameters.AddWithValue("@product_id", transaction.product_id);
            command.Parameters.AddWithValue("@warehouse_id", transaction.warehouse_id);
            command.Parameters.AddWithValue("@transaction_type", transaction.transaction_type);
            command.Parameters.AddWithValue("@quantity", transaction.quantity);
            command.Parameters.AddWithValue("@reference_no", (object?)transaction.reference_no ?? DBNull.Value);
            command.Parameters.AddWithValue("@reference_module", (object?)transaction.reference_module ?? DBNull.Value);
            command.Parameters.AddWithValue("@reference_id", (object?)transaction.reference_id ?? DBNull.Value);
            command.Parameters.AddWithValue("@remarks", (object?)transaction.remarks ?? DBNull.Value);
            command.Parameters.AddWithValue("@created_at", transaction.created_at);

            await command.ExecuteNonQueryAsync();
        }
    }
}