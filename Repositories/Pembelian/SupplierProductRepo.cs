using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface ISupplierProductRepo
    {
        Task<bool> InsertSupplierProduct(
            SupplierProduct model
        );

        Task<List<SupplierProduct>>
            GetAllSupplierProduct();

        Task<bool> BulkInsertSupplierProduct(
            List<SupplierProduct> models
        );

        Task<bool> DeductStock(int productId, int supplierId, int quantity);

        Task<bool> RestoreStock(int productId, int supplierId, int quantity);
    }

    public class SupplierProductRepo
        : ISupplierProductRepo
    {
        private readonly string _connectionString;

        public SupplierProductRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // ─── INSERT ─────────────────────────────

        public async Task<bool>
            InsertSupplierProduct(
                SupplierProduct model
            )
        {
            const string query = @"

                INSERT INTO supplier_products
                (
                    supplier_id,
                    product_id,
                    supplier_price,
                    available_stock,
                    lead_time_days,
                    is_available,
                    created_at
                )

                VALUES
                (
                    @supplier_id,
                    @product_id,
                    @supplier_price,
                    @available_stock,
                    @lead_time_days,
                    @is_available,
                    GETDATE()
                )";

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )

                using (
                    SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        )
                )
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@supplier_id",
                        model.supplier_id
                    );

                    command.Parameters.AddWithValue(
                        "@product_id",
                        model.product_id
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_price",
                        model.supplier_price
                    );

                    command.Parameters.AddWithValue(
                        "@available_stock",
                        model.available_stock
                    );

                    command.Parameters.AddWithValue(
                        "@lead_time_days",
                        model.lead_time_days
                    );

                    command.Parameters.AddWithValue(
                        "@is_available",
                        model.is_available
                    );

                    int result =
                        await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }

            catch (Exception)
            {
                return false;
            }
        }

        // ─── BULK INSERT ────────────────────────

        public async Task<bool>
            BulkInsertSupplierProduct(
                List<SupplierProduct> models
            )
        {
            const string query = @"

                INSERT INTO supplier_products
                (
                    supplier_id,
                    product_id,
                    supplier_price,
                    available_stock,
                    lead_time_days,
                    is_available,
                    created_at
                )

                VALUES
                (
                    @supplier_id,
                    @product_id,
                    @supplier_price,
                    @available_stock,
                    @lead_time_days,
                    @is_available,
                    @created_at
                )";

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )
                {
                    await connection.OpenAsync();

                    foreach (var model in models)
                    {
                        using (
                            SqlCommand command =
                                new SqlCommand(
                                    query,
                                    connection
                                )
                        )
                        {
                            command.Parameters.AddWithValue(
                                "@supplier_id",
                                model.supplier_id
                            );

                            command.Parameters.AddWithValue(
                                "@product_id",
                                model.product_id
                            );

                            command.Parameters.AddWithValue(
                                "@supplier_price",
                                model.supplier_price
                            );

                            command.Parameters.AddWithValue(
                                "@available_stock",
                                model.available_stock
                            );

                            command.Parameters.AddWithValue(
                                "@lead_time_days",
                                model.lead_time_days
                            );

                            command.Parameters.AddWithValue(
                                "@is_available",
                                model.is_available
                            );

                            command.Parameters.AddWithValue(
                                "@created_at",
                                model.created_at
                            );

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        // ─── GET ALL ────────────────────────────

        public async Task<List<SupplierProduct>>
            GetAllSupplierProduct()
        {
            const string query = @"

                SELECT
                    sp.*,

                    p.product_name,

                    p.uom_id,

                    s.supplier_name

                FROM supplier_products sp

                JOIN master_product p
                    ON sp.product_id = p.product_id

                JOIN master_supplier s
                    ON sp.supplier_id = s.supplier_id
            ";

            var response =
                new List<SupplierProduct>();

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )

                using (
                    SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        )
                )
                {
                    await connection.OpenAsync();

                    using (
                        SqlDataReader reader =
                            await command.ExecuteReaderAsync()
                    )
                    {
                        while (
                            await reader.ReadAsync()
                        )
                        {
                            var data =
                                new SupplierProduct()
                                {
                                    supplier_product_id =
                                        reader["supplier_product_id"] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader["supplier_product_id"]
                                        )
                                        : 0,

                                    supplier_id =
                                        reader["supplier_id"] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader["supplier_id"]
                                        )
                                        : 0,

                                    product_id =
                                        reader["product_id"] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader["product_id"]
                                        )
                                        : 0,

                                    supplier_price =
                                        reader["supplier_price"] != DBNull.Value
                                        ? Convert.ToDecimal(
                                            reader["supplier_price"]
                                        )
                                        : 0,

                                    available_stock =
                                        reader["available_stock"] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader["available_stock"]
                                        )
                                        : 0,

                                    lead_time_days =
                                        reader["lead_time_days"] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader["lead_time_days"]
                                        )
                                        : 0,

                                    is_available =
                                        reader["is_available"] != DBNull.Value
                                        && Convert.ToBoolean(
                                            reader["is_available"]
                                        ),

                                    created_at =
                                        reader["created_at"] != DBNull.Value
                                        ? Convert.ToDateTime(
                                            reader["created_at"]
                                        )
                                        : DateTime.Now,

                                    product_name =
                                        reader["product_name"]?.ToString(),

                                    supplier_name =
                                        reader["supplier_name"]?.ToString(),

                                    uom_id =
                                        reader["uom_id"] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader["uom_id"]
                                        )
                                        : 0
                                };

                            response.Add(data);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                var msg = ex.Message;

                throw new Exception(msg);
            }

            return response;
        }

        // ─── DEDUCT STOCK (hard reserve on PO approval) ──────────────────

        public async Task<bool> DeductStock(int productId, int supplierId, int quantity)
        {
            // Only deduct if enough stock exists; fail if it would go negative.
            const string query = @"
                UPDATE supplier_products
                SET available_stock = available_stock - @quantity
                WHERE product_id  = @product_id
                  AND supplier_id = @supplier_id
                  AND available_stock >= @quantity";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            command.Parameters.AddWithValue("@product_id",  productId);
            command.Parameters.AddWithValue("@supplier_id", supplierId);
            command.Parameters.AddWithValue("@quantity",    quantity);

            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // ─── RESTORE STOCK (undo reservation on return / unapprove) ──────

        public async Task<bool> RestoreStock(int productId, int supplierId, int quantity)
        {
            const string query = @"
                UPDATE supplier_products
                SET available_stock = available_stock + @quantity
                WHERE product_id  = @product_id
                  AND supplier_id = @supplier_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            command.Parameters.AddWithValue("@product_id",  productId);
            command.Parameters.AddWithValue("@supplier_id", supplierId);
            command.Parameters.AddWithValue("@quantity",    quantity);

            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}