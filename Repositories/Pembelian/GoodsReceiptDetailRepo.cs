using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IGoodsReceiptDetailRepo
    {
        Task<bool> InsertGoodsReceiptDetail(GoodsReceiptDetail model);

        Task<List<GoodsReceiptDetail>> GetDetailsByGoodsReceiptId(int goodsReceiptId);

        /// <summary>
        /// Same as GetDetailsByGoodsReceiptId but joins master_product to
        /// populate the product_name field — used by the Accept Loss modal.
        /// </summary>
        Task<List<GoodsReceiptDetail>> GetDetailsByGoodsReceiptIdWithProductName(int goodsReceiptId);

        /// <summary>
        /// Returns details (with product_name) only for lines where
        /// remaining_qty &gt; 0. Used by the Purchase Return creation modal.
        /// </summary>
        Task<List<GoodsReceiptDetail>> GetAvailableDetailsByGoodsReceiptId(int goodsReceiptId);

        /// <summary>
        /// Checks that every (goods_receipt_detail_id, requested_qty) pair
        /// has sufficient remaining_qty.  Returns a non-empty error message
        /// when any line fails; returns null when all lines pass.
        /// </summary>
        Task<string?> ValidateRemainingQty(
            int goodsReceiptId,
            IEnumerable<(int productId, int requestedQty)> lines
        );

        /// <summary>
        /// Atomically reduces remaining_qty for a single product line inside
        /// the given Goods Receipt.  Clamps at 0 and returns false if the
        /// requested reduction exceeds the current remaining_qty.
        /// </summary>
        Task<bool> ReduceRemainingQty(int goodsReceiptId, int productId, int qty);

        /// <summary>
        /// Deducts qty from inventory_stock for the given product (used when a
        /// Purchase Return sends goods back to the supplier) and re-syncs the
        /// supplier_products.available_stock mirror.
        /// </summary>
        Task<bool> DeductInventoryStock(int productId, int quantity);

        /// <summary>
        /// Reverses a prior deduction — adds back stock to inventory_stock
        /// and re-syncs the supplier_products mirror.
        /// Used when the supplier returns fixed goods (Accept Loss settlement).
        /// </summary>
        Task<bool> RestoreInventoryStock(int productId, int quantity);
    }

    public class GoodsReceiptDetailRepo : IGoodsReceiptDetailRepo
    {
        private readonly string _connectionString;

        public GoodsReceiptDetailRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public async Task<bool> InsertGoodsReceiptDetail(GoodsReceiptDetail model)
        {
            const string query = @"
                INSERT INTO goods_receipt_detail
                (
                    goods_receipt_id,
                    product_id,
                    quantity,
                    remaining_qty,
                    created_at
                )
                VALUES
                (
                    @goods_receipt_id,
                    @product_id,
                    @quantity,
                    @quantity,
                    DATEADD(HOUR, 7, GETUTCDATE())
                )";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@goods_receipt_id", model.goods_receipt_id);
                    command.Parameters.AddWithValue("@product_id", model.product_id);
                    command.Parameters.AddWithValue("@quantity", model.quantity);

                    int result = await command.ExecuteNonQueryAsync();

                    // ====================================================
                    // AUTO UPDATE INVENTORY STOCK
                    // ====================================================

                    const string checkStockQuery = @"
                        SELECT COUNT(*)
                        FROM inventory_stock
                        WHERE product_id = @product_id";

                    using (SqlCommand checkCommand =
                           new SqlCommand(checkStockQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue(
                            "@product_id",
                            model.product_id);

                        int stockExists =
                            (int)await checkCommand.ExecuteScalarAsync();

                        if (stockExists > 0)
                        {
                            const string updateStockQuery = @"
                                UPDATE inventory_stock
                                SET
                                    qty_on_hand = qty_on_hand + @quantity,
                                    qty_available = qty_available + @quantity,
                                    updated_at = DATEADD(HOUR, 7, GETUTCDATE())
                                WHERE product_id = @product_id";

                            using (SqlCommand updateCommand =
                                   new SqlCommand(updateStockQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue(
                                    "@quantity",
                                    model.quantity);

                                updateCommand.Parameters.AddWithValue(
                                    "@product_id",
                                    model.product_id);

                                await updateCommand.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // No inventory row yet — create one using warehouse 1
                            // (default warehouse). The persediaan/stok page can
                            // reassign to the correct warehouse afterwards.
                            const string insertStockQuery = @"
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
                                    1,
                                    @quantity,
                                    0,
                                    @quantity,
                                    DATEADD(HOUR, 7, GETUTCDATE()),
                                    DATEADD(HOUR, 7, GETUTCDATE())
                                )";

                            using (SqlCommand insertCommand =
                                   new SqlCommand(insertStockQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue(
                                    "@product_id",
                                    model.product_id);

                                insertCommand.Parameters.AddWithValue(
                                    "@quantity",
                                    model.quantity);

                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }

                        // Note: supplier_products.available_stock is intentionally
                        // NOT synced from inventory_stock here anymore -- it used to
                        // overwrite available_stock for every supplier selling this
                        // product_id (not just the one on this GR's PO), which kept
                        // clobbering freshly-uploaded catalog values. available_stock
                        // is now a pure snapshot maintained only by Excel upload;
                        // the effect of this GR on ordering capacity shows up via
                        // the live-computed reserved/available_to_order fields.
                    }

                    // ====================================================
                    // GET GOODS RECEIPT NUMBER
                    // ====================================================

                    string receiptNumber = "";

                    const string getReceiptNumberQuery = @"
                        SELECT receipt_number
                        FROM goods_receipt
                        WHERE goods_receipt_id = @goods_receipt_id";

                    using (SqlCommand receiptCommand =
                        new SqlCommand(getReceiptNumberQuery, connection))
                    {
                        receiptCommand.Parameters.AddWithValue(
                            "@goods_receipt_id",
                            model.goods_receipt_id);

                        var receiptResult =
                            await receiptCommand.ExecuteScalarAsync();

                        if (receiptResult != null &&
                            receiptResult != DBNull.Value)
                        {
                            receiptNumber = receiptResult.ToString()!;
                        }
                    }

                    // ====================================================
                    // INSERT STOCK TRANSACTION LOG
                    // ====================================================

                    const string insertTransactionQuery = @"
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
                            1,
                            'IN',
                            @quantity,
                            @reference_no,
                            'Goods Receipt',
                            @reference_id,
                            @remarks,
                            DATEADD(HOUR, 7, GETUTCDATE())
                        )";

                    using (SqlCommand transactionCommand =
                           new SqlCommand(insertTransactionQuery, connection))
                    {
                        transactionCommand.Parameters.AddWithValue(
                            "@product_id",
                            model.product_id);

                        transactionCommand.Parameters.AddWithValue(
                            "@quantity",
                            model.quantity);
                        
                        transactionCommand.Parameters.AddWithValue(
                            "@reference_no",
                            receiptNumber);

                        transactionCommand.Parameters.AddWithValue(
                            "@reference_id",
                            model.goods_receipt_id);

                        transactionCommand.Parameters.AddWithValue(
                            "@remarks",
                            "Stock added from Goods Receipt");

                        await transactionCommand.ExecuteNonQueryAsync();
                    }

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        // ─── GET DETAILS BY GOODS RECEIPT ID ────────────────────────────

        public async Task<List<GoodsReceiptDetail>> GetDetailsByGoodsReceiptId(int goodsReceiptId)
        {
            const string query = @"
                SELECT
                    goods_receipt_detail_id,
                    goods_receipt_id,
                    product_id,
                    quantity,
                    remaining_qty
                FROM goods_receipt_detail
                WHERE goods_receipt_id = @goods_receipt_id";

            var response = new List<GoodsReceiptDetail>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@goods_receipt_id", goodsReceiptId);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        response.Add(new GoodsReceiptDetail
                        {
                            goods_receipt_detail_id =
                                reader.GetInt32(reader.GetOrdinal("goods_receipt_detail_id")),
                            goods_receipt_id =
                                reader.GetInt32(reader.GetOrdinal("goods_receipt_id")),
                            product_id =
                                Convert.ToInt32(reader["product_id"]),
                            quantity =
                                reader.GetInt32(reader.GetOrdinal("quantity")),
                            remaining_qty =
                                reader.GetInt32(reader.GetOrdinal("remaining_qty"))
                        });
                    }
                }
            }

            return response;
        }
        // ─── GET DETAILS WITH PRODUCT NAME ──────────────────────────────

        public async Task<List<GoodsReceiptDetail>> GetDetailsByGoodsReceiptIdWithProductName(int goodsReceiptId)
        {
            const string query = @"
                SELECT
                    grd.goods_receipt_detail_id,
                    grd.goods_receipt_id,
                    grd.product_id,
                    grd.quantity,
                    grd.remaining_qty,
                    mp.product_name,
                    mu.uom_code
                FROM goods_receipt_detail grd
                INNER JOIN master_product mp
                    ON grd.product_id = mp.product_id
                LEFT JOIN master_uom mu
                    ON mu.uom_id = mp.uom_id
                WHERE grd.goods_receipt_id = @goods_receipt_id";

            var response = new List<GoodsReceiptDetail>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@goods_receipt_id", goodsReceiptId);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        response.Add(new GoodsReceiptDetail
                        {
                            goods_receipt_detail_id =
                                reader.GetInt32(reader.GetOrdinal("goods_receipt_detail_id")),
                            goods_receipt_id =
                                reader.GetInt32(reader.GetOrdinal("goods_receipt_id")),
                            product_id =
                                Convert.ToInt32(reader["product_id"]),
                            quantity =
                                reader.GetInt32(reader.GetOrdinal("quantity")),
                            remaining_qty =
                                reader.GetInt32(reader.GetOrdinal("remaining_qty")),
                            product_name =
                                reader["product_name"]?.ToString() ?? "",
                            uom_code =
                                reader["uom_code"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return response;
        }

        // ─── GET AVAILABLE DETAILS (remaining_qty > 0) ──────────────────

        /// <summary>
        /// Returns only lines where remaining_qty &gt; 0, with product_name populated.
        /// Used by the Purchase Return creation modal to filter out exhausted lines.
        /// </summary>
        public async Task<List<GoodsReceiptDetail>> GetAvailableDetailsByGoodsReceiptId(int goodsReceiptId)
        {
            const string query = @"
                SELECT
                    grd.goods_receipt_detail_id,
                    grd.goods_receipt_id,
                    grd.product_id,
                    grd.quantity,
                    grd.remaining_qty,
                    mp.product_name
                FROM goods_receipt_detail grd
                INNER JOIN master_product mp
                    ON grd.product_id = mp.product_id
                WHERE grd.goods_receipt_id = @goods_receipt_id
                  AND grd.remaining_qty > 0";

            var response = new List<GoodsReceiptDetail>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@goods_receipt_id", goodsReceiptId);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        response.Add(new GoodsReceiptDetail
                        {
                            goods_receipt_detail_id =
                                reader.GetInt32(reader.GetOrdinal("goods_receipt_detail_id")),
                            goods_receipt_id =
                                reader.GetInt32(reader.GetOrdinal("goods_receipt_id")),
                            product_id =
                                Convert.ToInt32(reader["product_id"]),
                            quantity =
                                reader.GetInt32(reader.GetOrdinal("quantity")),
                            remaining_qty =
                                reader.GetInt32(reader.GetOrdinal("remaining_qty")),
                            product_name =
                                reader["product_name"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return response;
        }

        // ─── VALIDATE REMAINING QTY ──────────────────────────────────────

        /// <summary>
        /// Checks that each (productId, requestedQty) pair does not exceed the
        /// current remaining_qty for that product in the given GR.
        /// Returns a human-readable error message on failure, or null on success.
        /// Call this inside a transaction before ReduceRemainingQty to guard
        /// against race conditions.
        /// </summary>
        public async Task<string?> ValidateRemainingQty(
            int goodsReceiptId,
            IEnumerable<(int productId, int requestedQty)> lines)
        {
            // Load current remaining quantities for all requested product lines
            const string query = @"
                SELECT product_id, remaining_qty
                FROM goods_receipt_detail
                WHERE goods_receipt_id = @goods_receipt_id
                  AND product_id IN (SELECT value FROM STRING_SPLIT(@product_ids, ','))";

            // Build the comma-separated product id list for the IN clause
            var lineList = lines.ToList();
            var productIdCsv = string.Join(",", lineList.Select(l => l.productId));

            if (string.IsNullOrEmpty(productIdCsv))
                return null; // nothing to validate

            var remainingByProduct = new Dictionary<int, int>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@goods_receipt_id", goodsReceiptId);
                command.Parameters.AddWithValue("@product_ids", productIdCsv);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        remainingByProduct[Convert.ToInt32(reader["product_id"])] =
                            reader.GetInt32(reader.GetOrdinal("remaining_qty"));
                    }
                }
            }

            foreach (var (productId, requestedQty) in lineList)
            {
                if (!remainingByProduct.TryGetValue(productId, out int available))
                    return $"Product ID {productId} was not found in Goods Receipt #{goodsReceiptId}.";

                if (requestedQty > available)
                    return
                        $"The requested return quantity ({requestedQty}) for product ID {productId} " +
                        $"exceeds the remaining quantity available in this Goods Receipt ({available}). " +
                        "The requested return quantity exceeds the remaining quantity available in this Goods Receipt.";
            }

            return null; // all lines valid
        }

        // ─── REDUCE REMAINING QTY ────────────────────────────────────────

        /// <summary>
        /// Reduces remaining_qty for a single product line in the given GR.
        /// Uses an optimistic UPDATE with a WHERE remaining_qty &gt;= @qty guard
        /// so a concurrent update cannot push the column below 0.
        /// Returns false if the row was not updated (insufficient remaining qty).
        /// </summary>
        public async Task<bool> ReduceRemainingQty(int goodsReceiptId, int productId, int qty)
        {
            const string query = @"
                UPDATE goods_receipt_detail
                SET remaining_qty = remaining_qty - @qty
                WHERE goods_receipt_id = @goods_receipt_id
                  AND product_id       = @product_id
                  AND remaining_qty   >= @qty";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@goods_receipt_id", goodsReceiptId);
            cmd.Parameters.AddWithValue("@product_id",       productId);
            cmd.Parameters.AddWithValue("@qty",              qty);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        // ─── DEDUCT INVENTORY STOCK (used by Purchase Return) ───────────
        // Reduces inventory_stock only. supplier_products.available_stock is no
        // longer re-synced here -- it's a pure catalog snapshot maintained only
        // by Excel upload; syncing it from inventory_stock by product_id alone
        // used to overwrite every supplier's row for that product, not just the
        // one on the originating PO/GR.

        public async Task<bool> DeductInventoryStock(int productId, int quantity)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string deductQuery = @"
                UPDATE inventory_stock
                SET
                    qty_on_hand   = CASE WHEN qty_on_hand   >= @quantity THEN qty_on_hand   - @quantity ELSE 0 END,
                    qty_available = CASE WHEN qty_available >= @quantity THEN qty_available - @quantity ELSE 0 END,
                    updated_at    = DATEADD(HOUR, 7, GETUTCDATE())
                WHERE product_id = @product_id";

            using SqlCommand cmd = new SqlCommand(deductQuery, connection);
            cmd.Parameters.AddWithValue("@product_id", productId);
            cmd.Parameters.AddWithValue("@quantity",   quantity);
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows >= 0;
        }

        // Adds stock back to inventory_stock -- used for Accept Loss settlement
        // when the supplier returns the same fixed goods. Mirrors
        // DeductInventoryStock in reverse (see note above re: supplier_products).
        public async Task<bool> RestoreInventoryStock(int productId, int quantity)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string restoreQuery = @"
                UPDATE inventory_stock
                SET
                    qty_on_hand   = qty_on_hand   + @quantity,
                    qty_available = qty_available + @quantity,
                    updated_at    = DATEADD(HOUR, 7, GETUTCDATE())
                WHERE product_id = @product_id";

            using SqlCommand cmd = new SqlCommand(restoreQuery, connection);
            cmd.Parameters.AddWithValue("@product_id", productId);
            cmd.Parameters.AddWithValue("@quantity",   quantity);
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows >= 0;
        }
    }
}