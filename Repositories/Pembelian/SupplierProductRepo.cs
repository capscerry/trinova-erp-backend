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

        Task<List<SupplierProduct>>
            GetProductsBySupplier(int supplierId);

        Task<bool> BulkInsertSupplierProduct(
            List<SupplierProduct> models
        );

        Task<bool> RestoreStock(int productId, int supplierId, int quantity);

        /// <summary>
        /// Outstanding quantity reserved against this supplier+product by
        /// Approved/Completed Purchase Orders that have not been fully
        /// received yet: SUM(PO qty - GR received qty), clamped at 0 per line.
        /// Does not read or write supplier_products.available_stock.
        /// </summary>
        Task<int> GetReservedQuantity(int supplierId, int productId);

        Task<bool> UpdateSupplierProduct(
            int supplierProductId,
            decimal supplierPrice,
            int availableStock,
            int leadTimeDays,
            bool isAvailable
        );

        Task<bool> DeleteSupplierProduct(int supplierProductId);
    }

    public class SupplierProductRepo
        : ISupplierProductRepo
    {
        private readonly string _connectionString;

        // Correlated scalar subquery: outstanding qty reserved against the outer
        // row's (supplier_id, product_id) by Approved/Completed POs that have
        // not been fully received. References sp.supplier_id / sp.product_id
        // from whatever outer query embeds it -- the outer query must alias
        // supplier_products as "sp". Only one Goods Receipt per PO is possible
        // today (enforced elsewhere), so summing goods_receipt_detail.quantity
        // per (purchase_order_id, product_id) is safe.
        private const string ReservedQuantityCorrelatedSubquery = @"
            (
                SELECT ISNULL(SUM(
                    CASE WHEN (pod.quantity - ISNULL(grq.received_qty, 0)) > 0
                         THEN (pod.quantity - ISNULL(grq.received_qty, 0))
                         ELSE 0
                    END
                ), 0)
                FROM purchase_order_detail pod
                JOIN purchase_order po ON po.purchase_order_id = pod.purchase_order_id
                OUTER APPLY (
                    SELECT SUM(grd.quantity) AS received_qty
                    FROM goods_receipt gr
                    JOIN goods_receipt_detail grd ON grd.goods_receipt_id = gr.goods_receipt_id
                    WHERE gr.purchase_order_id = pod.purchase_order_id
                      AND grd.product_id = pod.product_id
                ) grq
                WHERE po.supplier_id = sp.supplier_id
                  AND pod.product_id = sp.product_id
                  AND po.status IN ('Approved', 'Completed')
            )";

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
                    DATEADD(HOUR, 7, GETUTCDATE())
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

                    // Single transaction for the whole batch instead of one
                    // implicit auto-commit per row -- large catalogs (hundreds
                    // of rows) were slow to upload because every MERGE round-tripped
                    // its own commit to Azure SQL individually.
                    using (
                        SqlTransaction transaction =
                            connection.BeginTransaction()
                    )
                    {
                        foreach (var model in models)
                        {
                            using (
                                SqlCommand command =
                                    new SqlCommand(
                                        query,
                                        connection,
                                        transaction
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

                        transaction.Commit();
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
            string query = @"

                SELECT
                    sp.*,

                    p.product_name,

                    p.uom_id,

                    s.supplier_name,

                    " + ReservedQuantityCorrelatedSubquery + @" AS reserved_quantity

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
                        // Performance: cache ordinal positions once before the
                        // read loop. GetOrdinal does a linear string scan — calling
                        // it per-row on 11 columns wastes CPU for large catalogs.
                        // Mapping behaviour and returned model are unchanged.
                        int ord_supplier_product_id = reader.GetOrdinal("supplier_product_id");
                        int ord_supplier_id         = reader.GetOrdinal("supplier_id");
                        int ord_product_id          = reader.GetOrdinal("product_id");
                        int ord_supplier_price      = reader.GetOrdinal("supplier_price");
                        int ord_available_stock     = reader.GetOrdinal("available_stock");
                        int ord_lead_time_days      = reader.GetOrdinal("lead_time_days");
                        int ord_is_available        = reader.GetOrdinal("is_available");
                        int ord_created_at          = reader.GetOrdinal("created_at");
                        int ord_product_name        = reader.GetOrdinal("product_name");
                        int ord_supplier_name       = reader.GetOrdinal("supplier_name");
                        int ord_uom_id              = reader.GetOrdinal("uom_id");
                        int ord_reserved_quantity   = reader.GetOrdinal("reserved_quantity");

                        while (
                            await reader.ReadAsync()
                        )
                        {
                            var availableStock =
                                reader[ord_available_stock] != DBNull.Value
                                ? Convert.ToInt32(reader[ord_available_stock])
                                : 0;

                            var reservedQuantity =
                                reader[ord_reserved_quantity] != DBNull.Value
                                ? Convert.ToInt32(reader[ord_reserved_quantity])
                                : 0;

                            var data =
                                new SupplierProduct()
                                {
                                    supplier_product_id =
                                        reader[ord_supplier_product_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_supplier_product_id]
                                        )
                                        : 0,

                                    supplier_id =
                                        reader[ord_supplier_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_supplier_id]
                                        )
                                        : 0,

                                    product_id =
                                        reader[ord_product_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_product_id]
                                        )
                                        : 0,

                                    supplier_price =
                                        reader[ord_supplier_price] != DBNull.Value
                                        ? Convert.ToDecimal(
                                            reader[ord_supplier_price]
                                        )
                                        : 0,

                                    available_stock = availableStock,

                                    lead_time_days =
                                        reader[ord_lead_time_days] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_lead_time_days]
                                        )
                                        : 0,

                                    is_available =
                                        reader[ord_is_available] != DBNull.Value
                                        && Convert.ToBoolean(
                                            reader[ord_is_available]
                                        ),

                                    created_at =
                                        reader[ord_created_at] != DBNull.Value
                                        ? Convert.ToDateTime(
                                            reader[ord_created_at]
                                        )
                                        : DateTime.UtcNow.AddHours(7),

                                    product_name =
                                        reader[ord_product_name]?.ToString(),

                                    supplier_name =
                                        reader[ord_supplier_name]?.ToString(),

                                    uom_id =
                                        reader[ord_uom_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_uom_id]
                                        )
                                        : 0,

                                    reserved_quantity = reservedQuantity,

                                    available_to_order =
                                        Math.Max(availableStock - reservedQuantity, 0)
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
            string query = @"

                SELECT
                    sp.*,

                    p.product_name,

                    p.uom_id,

                    s.supplier_name,

                    " + ReservedQuantityCorrelatedSubquery + @" AS reserved_quantity

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
                        // Performance: same ordinal-caching pattern as GetAllSupplierProduct.
                        // GetOrdinal is called once per query, not once per row.
                        // Mapping behaviour and returned model are unchanged.
                        int ord_supplier_product_id = reader.GetOrdinal("supplier_product_id");
                        int ord_supplier_id         = reader.GetOrdinal("supplier_id");
                        int ord_product_id          = reader.GetOrdinal("product_id");
                        int ord_supplier_price      = reader.GetOrdinal("supplier_price");
                        int ord_available_stock     = reader.GetOrdinal("available_stock");
                        int ord_lead_time_days      = reader.GetOrdinal("lead_time_days");
                        int ord_is_available        = reader.GetOrdinal("is_available");
                        int ord_created_at          = reader.GetOrdinal("created_at");
                        int ord_product_name        = reader.GetOrdinal("product_name");
                        int ord_supplier_name       = reader.GetOrdinal("supplier_name");
                        int ord_uom_id              = reader.GetOrdinal("uom_id");
                        int ord_reserved_quantity   = reader.GetOrdinal("reserved_quantity");

                        while (
                            await reader.ReadAsync()
                        )
                        {
                            var availableStock =
                                reader[ord_available_stock] != DBNull.Value
                                ? Convert.ToInt32(reader[ord_available_stock])
                                : 0;

                            var reservedQuantity =
                                reader[ord_reserved_quantity] != DBNull.Value
                                ? Convert.ToInt32(reader[ord_reserved_quantity])
                                : 0;

                            var data =
                                new SupplierProduct()
                                {
                                    supplier_product_id =
                                        reader[ord_supplier_product_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_supplier_product_id]
                                        )
                                        : 0,

                                    supplier_id =
                                        reader[ord_supplier_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_supplier_id]
                                        )
                                        : 0,

                                    product_id =
                                        reader[ord_product_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_product_id]
                                        )
                                        : 0,

                                    supplier_price =
                                        reader[ord_supplier_price] != DBNull.Value
                                        ? Convert.ToDecimal(
                                            reader[ord_supplier_price]
                                        )
                                        : 0,

                                    available_stock = availableStock,

                                    lead_time_days =
                                        reader[ord_lead_time_days] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_lead_time_days]
                                        )
                                        : 0,

                                    is_available =
                                        reader[ord_is_available] != DBNull.Value
                                        && Convert.ToBoolean(
                                            reader[ord_is_available]
                                        ),

                                    created_at =
                                        reader[ord_created_at] != DBNull.Value
                                        ? Convert.ToDateTime(
                                            reader[ord_created_at]
                                        )
                                        : DateTime.UtcNow.AddHours(7),

                                    product_name =
                                        reader[ord_product_name]?.ToString(),

                                    supplier_name =
                                        reader[ord_supplier_name]?.ToString(),

                                    uom_id =
                                        reader[ord_uom_id] != DBNull.Value
                                        ? Convert.ToInt32(
                                            reader[ord_uom_id]
                                        )
                                        : 0,

                                    reserved_quantity = reservedQuantity,

                                    available_to_order =
                                        Math.Max(availableStock - reservedQuantity, 0)
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

        // ─── RESTORE STOCK (manual admin action via /restore-stock) ──────

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

        // ─── RESERVED QUANTITY (open PO qty not yet received) ────────────
        // Same logic as ReservedQuantityCorrelatedSubquery, but as a standalone
        // parameterized query for a single (supplier_id, product_id) pair.

        public async Task<int> GetReservedQuantity(int supplierId, int productId)
        {
            const string query = @"
                SELECT ISNULL(SUM(
                    CASE WHEN (pod.quantity - ISNULL(grq.received_qty, 0)) > 0
                         THEN (pod.quantity - ISNULL(grq.received_qty, 0))
                         ELSE 0
                    END
                ), 0)
                FROM purchase_order_detail pod
                JOIN purchase_order po ON po.purchase_order_id = pod.purchase_order_id
                OUTER APPLY (
                    SELECT SUM(grd.quantity) AS received_qty
                    FROM goods_receipt gr
                    JOIN goods_receipt_detail grd ON grd.goods_receipt_id = gr.goods_receipt_id
                    WHERE gr.purchase_order_id = pod.purchase_order_id
                      AND grd.product_id = pod.product_id
                ) grq
                WHERE po.supplier_id = @supplier_id
                  AND pod.product_id = @product_id
                  AND po.status IN ('Approved', 'Completed')";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            command.Parameters.AddWithValue("@supplier_id", supplierId);
            command.Parameters.AddWithValue("@product_id", productId);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        // ─── UPDATE (fix a duplicate/incorrect catalog row) ──────────────

        public async Task<bool> UpdateSupplierProduct(
            int supplierProductId,
            decimal supplierPrice,
            int availableStock,
            int leadTimeDays,
            bool isAvailable
        )
        {
            const string query = @"
                UPDATE supplier_products
                SET supplier_price  = @supplier_price,
                    available_stock = @available_stock,
                    lead_time_days  = @lead_time_days,
                    is_available    = @is_available
                WHERE supplier_product_id = @supplier_product_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            command.Parameters.AddWithValue("@supplier_product_id", supplierProductId);
            command.Parameters.AddWithValue("@supplier_price", supplierPrice);
            command.Parameters.AddWithValue("@available_stock", availableStock);
            command.Parameters.AddWithValue("@lead_time_days", leadTimeDays);
            command.Parameters.AddWithValue("@is_available", isAvailable);

            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // ─── DELETE (remove a stray duplicate catalog row) ────────────────

        public async Task<bool> DeleteSupplierProduct(int supplierProductId)
        {
            const string query = @"
                DELETE FROM supplier_products
                WHERE supplier_product_id = @supplier_product_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            command.Parameters.AddWithValue("@supplier_product_id", supplierProductId);

            int rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}