using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseReturnRepo
    {
        Task<string> GenerateReturnNumber();

        Task<int> InsertPurchaseReturn(PurchaseReturn model);

        Task<List<PurchaseReturn>> GetAllPurchaseReturn();

        Task<bool> UpdatePurchaseReturn(int id, string status, string notes, string closingCondition);

        Task<bool> DeletePurchaseReturn(int id);
    }

    public class PurchaseReturnRepo : IPurchaseReturnRepo
    {
        private readonly string _connectionString;

        public PurchaseReturnRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // GENERATE RETURN NUMBER
        public async Task<string> GenerateReturnNumber()
        {
            const string query = @"
                SELECT TOP 1 purchase_return_number
                FROM purchase_return
                ORDER BY purchase_return_id DESC";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using SqlCommand command =
                new SqlCommand(query, connection);

            object? result =
                await command.ExecuteScalarAsync();

            int nextNumber = 1;

            if (result != null && result != DBNull.Value)
            {
                string last = result.ToString() ?? "RTN-0000000000";
                string numericPart = last.Replace("RTN-", "").Replace("PR-", "");

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"RTN-{nextNumber:D10}";
        }

        // INSERT
        public async Task<int> InsertPurchaseReturn(PurchaseReturn model)
        {
            const string query = @"
                INSERT INTO purchase_return
                (
                    goods_receipt_id,
                    purchase_return_number,
                    return_date,
                    supplier_name,
                    purchase_order_number,
                    total_amount,
                    settlement_option,
                    notes,
                    status,
                    closing_condition,
                    transaction_name,
                    transaction_detail,
                    created_at
                )
                VALUES
                (
                    @goods_receipt_id,
                    @purchase_return_number,
                    @return_date,
                    @supplier_name,
                    @purchase_order_number,
                    @total_amount,
                    @settlement_option,
                    @notes,
                    @status,
                    @closing_condition,
                    @transaction_name,
                    @transaction_detail,
                    GETDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                command.Parameters.AddWithValue("@goods_receipt_id",       model.goods_receipt_id);
                command.Parameters.AddWithValue("@purchase_return_number", model.purchase_return_number);
                command.Parameters.AddWithValue("@return_date",            model.return_date);
                command.Parameters.AddWithValue("@supplier_name",          model.supplier_name);
                command.Parameters.AddWithValue("@purchase_order_number",  model.purchase_order_number);
                command.Parameters.AddWithValue("@total_amount",           model.total_amount);
                command.Parameters.AddWithValue("@settlement_option",      model.settlement_option);
                command.Parameters.AddWithValue("@notes",                  model.notes);
                command.Parameters.AddWithValue("@status",                 model.status);
                command.Parameters.AddWithValue("@closing_condition",      model.closing_condition);
                command.Parameters.AddWithValue("@transaction_name",       model.transaction_name);
                command.Parameters.AddWithValue("@transaction_detail",     model.transaction_detail);

                int id = Convert.ToInt32(await command.ExecuteScalarAsync());
                return id;
            }
        }

        // GET ALL
        public async Task<List<PurchaseReturn>> GetAllPurchaseReturn()
        {
            const string query = @"
                SELECT *
                FROM purchase_return
                ORDER BY purchase_return_id DESC";

            var response = new List<PurchaseReturn>();

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                using (SqlDataReader reader =
                    await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        response.Add(new PurchaseReturn
                        {
                            purchase_return_id =
                                Convert.ToInt32(reader["purchase_return_id"]),

                            goods_receipt_id =
                                Convert.ToInt32(reader["goods_receipt_id"]),

                            purchase_return_number =
                                reader["purchase_return_number"]?.ToString() ?? "",

                            return_date =
                                Convert.ToDateTime(reader["return_date"]),

                            supplier_name =
                                reader["supplier_name"]?.ToString() ?? "",

                            purchase_order_number =
                                reader["purchase_order_number"]?.ToString() ?? "",

                            total_amount =
                                Convert.ToDecimal(reader["total_amount"]),

                            settlement_option =
                                reader["settlement_option"]?.ToString() ?? "",

                            notes =
                                reader["notes"]?.ToString() ?? "",

                            status =
                                reader["status"]?.ToString() ?? "",

                            closing_condition =
                                reader["closing_condition"]?.ToString() ?? "",

                            transaction_name =
                                reader["transaction_name"]?.ToString() ?? "",

                            transaction_detail =
                                reader["transaction_detail"]?.ToString() ?? "",

                            created_at =
                                reader["created_at"] != DBNull.Value
                                ? Convert.ToDateTime(reader["created_at"])
                                : null,
                        });
                    }
                }
            }

            return response;
        }

        // UPDATE (status + notes + closing_condition — used by settlement actions)
        public async Task<bool> UpdatePurchaseReturn(int id, string status, string notes, string closingCondition)
        {
            const string query = @"
                UPDATE purchase_return
                SET
                    status            = @status,
                    notes             = @notes,
                    closing_condition = @closing_condition
                WHERE purchase_return_id = @id";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@id",                id);
                command.Parameters.AddWithValue("@status",            status);
                command.Parameters.AddWithValue("@notes",             notes);
                command.Parameters.AddWithValue("@closing_condition", closingCondition);
                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }

        // DELETE
        public async Task<bool> DeletePurchaseReturn(int id)
        {
            const string query = @"
                DELETE FROM purchase_return
                WHERE purchase_return_id = @id";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@id", id);
                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
        }
    }
}
