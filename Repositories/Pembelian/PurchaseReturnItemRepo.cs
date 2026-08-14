using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseReturnItemRepo
    {
        Task InsertBatch(int purchaseReturnId, IEnumerable<PurchaseReturnItem> items);
        Task<List<PurchaseReturnItem>> GetByReturnId(int purchaseReturnId);
    }

    public class PurchaseReturnItemRepo : IPurchaseReturnItemRepo
    {
        private readonly string _connectionString;

        public PurchaseReturnItemRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // INSERT BATCH — called once after the purchase_return header is created
        public async Task InsertBatch(int purchaseReturnId, IEnumerable<PurchaseReturnItem> items)
        {
            const string query = @"
                INSERT INTO purchase_return_item
                (
                    purchase_return_id,
                    product_id,
                    product_name,
                    qty_return,
                    unit_price,
                    subtotal
                )
                VALUES
                (
                    @purchase_return_id,
                    @product_id,
                    @product_name,
                    @qty_return,
                    @unit_price,
                    @subtotal
                )";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var item in items)
            {
                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@purchase_return_id", purchaseReturnId);
                command.Parameters.AddWithValue("@product_id",         item.product_id);
                command.Parameters.AddWithValue("@product_name",       item.product_name);
                command.Parameters.AddWithValue("@qty_return",         item.qty_return);
                command.Parameters.AddWithValue("@unit_price",         item.unit_price);
                command.Parameters.AddWithValue("@subtotal",           item.subtotal);
                await command.ExecuteNonQueryAsync();
            }
        }

        // GET BY RETURN ID — used by the settlement modal and stock-restore logic
        public async Task<List<PurchaseReturnItem>> GetByReturnId(int purchaseReturnId)
        {
            const string query = @"
                SELECT *
                FROM purchase_return_item
                WHERE purchase_return_id = @purchase_return_id
                ORDER BY purchase_return_item_id";

            var result = new List<PurchaseReturnItem>();

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);
            await connection.OpenAsync();

            command.Parameters.AddWithValue("@purchase_return_id", purchaseReturnId);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new PurchaseReturnItem
                {
                    purchase_return_item_id =
                        Convert.ToInt32(reader["purchase_return_item_id"]),
                    purchase_return_id =
                        Convert.ToInt32(reader["purchase_return_id"]),
                    product_id =
                        Convert.ToInt32(reader["product_id"]),
                    product_name =
                        reader["product_name"]?.ToString() ?? "",
                    qty_return =
                        Convert.ToInt32(reader["qty_return"]),
                    unit_price =
                        Convert.ToDecimal(reader["unit_price"]),
                    subtotal =
                        Convert.ToDecimal(reader["subtotal"]),
                });
            }

            return result;
        }
    }
}
