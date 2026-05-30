using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IGoodsReceiptDetailRepo
    {
        Task<bool> InsertGoodsReceiptDetail(GoodsReceiptDetail model);
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
                    created_at
                )
                VALUES
                (
                    @goods_receipt_id,
                    @product_id,
                    @quantity,
                    GETDATE()
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
                                    updated_at = GETDATE()
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
                        // else
                        // {
                        //     const string insertStockQuery = @"
                        //         INSERT INTO inventory_stock
                        //         (
                        //             product_id,
                        //             quantity,
                        //             minimum_stock,
                        //             maximum_stock,
                        //             created_at,
                        //             updated_at
                        //         )
                        //         VALUES
                        //         (
                        //             @product_id,
                        //             @quantity,
                        //             0,
                        //             0,
                        //             GETDATE(),
                        //             GETDATE()
                        //         )";

                        //     using (SqlCommand insertCommand =
                        //            new SqlCommand(insertStockQuery, connection))
                        //     {
                        //         insertCommand.Parameters.AddWithValue(
                        //             "@product_id",
                        //             model.product_id);

                        //         insertCommand.Parameters.AddWithValue(
                        //             "@quantity",
                        //             model.quantity);

                        //         await insertCommand.ExecuteNonQueryAsync();
                        //     }
                        // }
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
                            'Goods Receipt',
                            @reference_id,
                            @remarks,
                            GETDATE()
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
    }
}