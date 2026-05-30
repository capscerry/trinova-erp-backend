using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class InventoryStockRepo
    {
        private readonly string _connectionString;

        public InventoryStockRepo(
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
        public async Task<List<InventoryStock>> GetAllAsync()
        {
            const string query = @"
                SELECT
                    s.*,

                    p.product_id AS p_product_id,
                    p.product_name,
                    p.product_code,
                    p.product_type,

                    w.warehouse_id AS w_warehouse_id,
                    w.warehouse_name

                FROM inventory_stock s

                LEFT JOIN master_product p
                    ON s.product_id = p.product_id

                LEFT JOIN master_warehouse w
                    ON s.warehouse_id = w.warehouse_id

                ORDER BY s.stock_id DESC";

            var response = new List<InventoryStock>();

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(
                    new InventoryStock
                    {
                        stock_id = Convert.ToInt32(reader["stock_id"]),
                        product_id = Convert.ToInt32(reader["product_id"]),
                        warehouse_id = Convert.ToInt32(reader["warehouse_id"]),

                        qty_on_hand = Convert.ToDecimal(reader["qty_on_hand"]),
                        qty_reserved = Convert.ToDecimal(reader["qty_reserved"]),
                        qty_available = Convert.ToDecimal(reader["qty_available"]),

                        created_at = reader["created_at"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["created_at"]),

                        updated_at = reader["updated_at"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(reader["updated_at"]),

                        Product = new MasterProduct
                        {
                            product_id = Convert.ToInt32(reader["p_product_id"]),
                            product_name = reader["product_name"]?.ToString(),
                            product_code = reader["product_code"]?.ToString(),
                            product_type = reader["product_type"]?.ToString()
                        },

                        Warehouse = new MasterWarehouse
                        {
                            warehouse_id = Convert.ToInt32(reader["w_warehouse_id"]),
                            warehouse_name = reader["warehouse_name"]?.ToString()
                        }
                    }
                );
            }

            return response;
        }

        // =========================
        // GET BY ID
        // =========================
        public async Task<InventoryStock?> GetByIdAsync(int id)
        {
            const string query = @"
                SELECT
                    s.*,

                    p.product_id AS p_product_id,
                    p.product_name,
                    p.product_code,
                    p.product_type,

                    w.warehouse_id AS w_warehouse_id,
                    w.warehouse_name

                FROM inventory_stock s

                LEFT JOIN master_product p
                    ON s.product_id = p.product_id

                LEFT JOIN master_warehouse w
                    ON s.warehouse_id = w.warehouse_id

                WHERE s.stock_id = @stock_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@stock_id", id);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new InventoryStock
            {
                stock_id = Convert.ToInt32(reader["stock_id"]),
                product_id = Convert.ToInt32(reader["product_id"]),
                warehouse_id = Convert.ToInt32(reader["warehouse_id"]),

                qty_on_hand = Convert.ToDecimal(reader["qty_on_hand"]),
                qty_reserved = Convert.ToDecimal(reader["qty_reserved"]),
                qty_available = Convert.ToDecimal(reader["qty_available"]),

                created_at = reader["created_at"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["created_at"]),

                updated_at = reader["updated_at"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(reader["updated_at"]),

                Product = new MasterProduct
                {
                    product_id = Convert.ToInt32(reader["p_product_id"]),
                    product_name = reader["product_name"]?.ToString(),
                    product_code = reader["product_code"]?.ToString(),
                    product_type = reader["product_type"]?.ToString()
                },

                Warehouse = new MasterWarehouse
                {
                    warehouse_id = Convert.ToInt32(reader["w_warehouse_id"]),
                    warehouse_name = reader["warehouse_name"]?.ToString()
                }
            };
        }

        // =========================
        // CREATE
        // =========================
        public async Task<InventoryStock> CreateAsync(
            InventoryStock stock
        )
        {
            const string query = @"
                INSERT INTO inventory_stock
                (
                    product_id,
                    warehouse_id,
                    qty_on_hand,
                    qty_reserved,
                    qty_available,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @product_id,
                    @warehouse_id,
                    @qty_on_hand,
                    @qty_reserved,
                    @qty_available,
                    @created_at,
                    @updated_at
                )";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@product_id", stock.product_id);
            command.Parameters.AddWithValue("@warehouse_id", stock.warehouse_id);
            command.Parameters.AddWithValue("@qty_on_hand", stock.qty_on_hand);
            command.Parameters.AddWithValue("@qty_reserved", stock.qty_reserved);
            command.Parameters.AddWithValue("@qty_available", stock.qty_available);
            command.Parameters.AddWithValue("@created_at", stock.created_at ?? DateTime.Now);
            command.Parameters.AddWithValue("@updated_at", stock.updated_at ?? DateTime.Now);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();

            return stock;
        }

        // =========================
        // UPDATE
        // =========================
        public async Task UpdateAsync(
            InventoryStock stock
        )
        {
            const string query = @"
                UPDATE inventory_stock
                SET
                    product_id = @product_id,
                    warehouse_id = @warehouse_id,
                    qty_on_hand = @qty_on_hand,
                    qty_reserved = @qty_reserved,
                    qty_available = @qty_available,
                    updated_at = @updated_at
                WHERE stock_id = @stock_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@stock_id", stock.stock_id);
            command.Parameters.AddWithValue("@product_id", stock.product_id);
            command.Parameters.AddWithValue("@warehouse_id", stock.warehouse_id);
            command.Parameters.AddWithValue("@qty_on_hand", stock.qty_on_hand);
            command.Parameters.AddWithValue("@qty_reserved", stock.qty_reserved);
            command.Parameters.AddWithValue("@qty_available", stock.qty_available);
            command.Parameters.AddWithValue("@updated_at", DateTime.Now);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();
        }

        // =========================
        // DELETE
        // =========================
        public async Task DeleteAsync(
            InventoryStock stock
        )
        {
            const string query = @"
                DELETE FROM inventory_stock
                WHERE stock_id = @stock_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@stock_id", stock.stock_id);

            await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();
        }

        public async Task<InventoryStock?> GetByProductWarehouseAsync(
            int productId,
            int warehouseId)
        {
            const string query = @"
                SELECT *
                FROM inventory_stock
                WHERE product_id = @product_id
                AND warehouse_id = @warehouse_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@product_id",
                productId);

            command.Parameters.AddWithValue(
                "@warehouse_id",
                warehouseId);

            await connection.OpenAsync();

            using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new InventoryStock
            {
                stock_id = Convert.ToInt32(reader["stock_id"]),
                product_id = Convert.ToInt32(reader["product_id"]),
                warehouse_id = Convert.ToInt32(reader["warehouse_id"]),
                qty_on_hand = Convert.ToDecimal(reader["qty_on_hand"]),
                qty_reserved = Convert.ToDecimal(reader["qty_reserved"]),
                qty_available = Convert.ToDecimal(reader["qty_available"])
            };
        }
    }
}