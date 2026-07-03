using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    /// <summary>
    /// Result of a DeductStock call.
    /// <list type="bullet">
    ///   <item><term>Deducted</term><description>Row found and stock successfully decremented.</description></item>
    ///   <item><term>InsufficientStock</term><description>Row found but available_stock &lt; requested quantity.</description></item>
    ///   <item><term>RowNotFound</term><description>No supplier_products row exists for this (product_id, supplier_id) pair — skip silently.</description></item>
    /// </list>
    /// </summary>
    public enum DeductStockResult
    {
        Deducted,
        InsufficientStock,
        RowNotFound,
    }

    public interface ISupplierProductRepo
    {
        Task<bool> InsertSupplierProduct(
            SupplierProduct model
        );

        Task<List<SupplierProduct>>
            GetAllSupplierProduct();

        Task<List<SupplierProduct>>
            GetProductsBySupplier(int supplierId);

        Task<bool> BulkInsertSupplierProduct(
            List<SupplierProduct> models
        );

        Task<DeductStockResult> DeductStock(int productId, int supplierId, int quantity);

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
            // MERGE so that re-uploading the same catalog updates existing rows
            // instead of creating duplicates. Keyed on (supplier_id, product_id).
            const string query = @"

                MERGE supplier_products AS target

                USING (
                    SELECT
                        @supplier_id     AS supplier_id,
                        @product_id      AS product_id,
                        @supplier_price  AS supplier_price,
                        @available_stock AS available_stock,
                        @lead_time_days  AS lead_time_days,
                        @is_available    AS is_available,
                        @created_at      AS created_at
                ) AS source
                ON  target.supplier_id = source.supplier_id
                AND target.product_id  = source.product_id

                WHEN MATCHED THEN
                    UPDATE SET
                        supplier_price  = source.supplier_price,
                        available_stock = source.available_stock,
                        lead_time_days  = source.lead_time_days,
                        is_available    = source.is_available

                WHEN NOT MATCHED THEN
                    INSERT (
                        supplier_id,
                        product_id,
                        supplier_price,
                        available_stock,
                        lead_time_days,
                        is_available,
                        created_at
                    )
                    VALUES (
                        source.supplier_id,
                        source.product_id,
                        source.supplier_price,
                        source.available_stock,
                        source.lead_time_days,
                        source.is_available,
                        source.created_at
                    );
            ";

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

        // ─── GET BY SUPPLIER ────────────────────────────

        public async Task<List<SupplierProduct>>
            GetProductsBySupplier(int supplierId)
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

                WHERE sp.supplier_id = @supplier_id
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

                    command.Parameters.AddWithValue(
                        "@supplier_id",
                        supplierId
                    );

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

        public async Task<DeductStockResult> DeductStock(int productId, int supplierId, int quantity)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // First check whether a row even exists for this (product, supplier) pair.
            const string existsQuery = @"
                SELECT COUNT(1)
                FROM supplier_products
                WHERE product_id  = @product_id
                  AND supplier_id = @supplier_id";

            using (SqlCommand existsCmd = new SqlCommand(existsQuery, connection))
            {
                existsCmd.Parameters.AddWithValue("@product_id",  productId);
                existsCmd.Parameters.AddWithValue("@supplier_id", supplierId);

                var count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                if (count == 0)
                    return DeductStockResult.RowNotFound;
            }

            // Row exists — attempt the conditional deduction.
            // The WHERE clause only matches when available_stock >= quantity,
            // so 0 rows affected means insufficient stock (not a missing row).
            const string deductQuery = @"
                UPDATE supplier_products
                SET available_stock = available_stock - @quantity
                WHERE product_id  = @product_id
                  AND supplier_id = @supplier_id
                  AND available_stock >= @quantity";

            using SqlCommand deductCmd = new SqlCommand(deductQuery, connection);
            deductCmd.Parameters.AddWithValue("@product_id",  productId);
            deductCmd.Parameters.AddWithValue("@supplier_id", supplierId);
            deductCmd.Parameters.AddWithValue("@quantity",    quantity);

            int rows = await deductCmd.ExecuteNonQueryAsync();
            return rows > 0 ? DeductStockResult.Deducted : DeductStockResult.InsufficientStock;
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