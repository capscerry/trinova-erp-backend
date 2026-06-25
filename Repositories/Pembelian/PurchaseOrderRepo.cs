using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseOrderRepo
    {
        Task<string> GeneratePONumber();

        Task<int> InsertPurchaseOrder(PurchaseOrder model);

        Task<List<PurchaseOrder>> GetAllPurchaseOrder();

        Task<PurchaseOrder?> GetPurchaseOrderById(int id);

        Task<bool> UpdatePurchaseOrder(PurchaseOrder model);

        Task<bool> DeletePurchaseOrder(int id);
    }

    public class PurchaseOrderRepo : IPurchaseOrderRepo
    {
        private readonly string _connectionString;

        public PurchaseOrderRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        // GENERATE PO NUMBER
        public async Task<string> GeneratePONumber()
        {
            const string query = @"
                SELECT TOP 1 po_number
                FROM purchase_order
                ORDER BY purchase_order_id DESC";

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
                string lastPo =
                    result.ToString() ?? "PO-0000000000";

                string numericPart =
                    lastPo.Replace("PO-", "");

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"PO-{nextNumber:D10}";
        }

        // INSERT
        public async Task<int> InsertPurchaseOrder(PurchaseOrder model)
        {
            const string query = @"
                INSERT INTO purchase_order
                (
                    po_number,
                    supplier_id,
                    order_date,
                    status,
                    total_amount,
                    created_at
                )
                OUTPUT INSERTED.purchase_order_id
                VALUES
                (
                    @po_number,
                    @supplier_id,
                    @order_date,
                    @status,
                    @total_amount,
                    GETDATE()
                )";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@po_number", model.po_number);
                    command.Parameters.AddWithValue("@supplier_id", model.supplier_id);
                    command.Parameters.AddWithValue("@order_date", model.order_date);
                    command.Parameters.AddWithValue("@status", model.status);
                    command.Parameters.AddWithValue("@total_amount", model.total_amount);

                    int insertedId =
                        Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );

                    return insertedId;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // UPDATE
        public async Task<bool> UpdatePurchaseOrder(PurchaseOrder model)
        {
            const string query = @"
                UPDATE purchase_order
                SET
                    po_number = @po_number,
                    supplier_id = @supplier_id,
                    order_date = @order_date,
                    status = @status,
                    total_amount = @total_amount
                WHERE purchase_order_id = @purchase_order_id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@purchase_order_id", model.purchase_order_id);
                    command.Parameters.AddWithValue("@po_number", model.po_number);
                    command.Parameters.AddWithValue("@supplier_id", model.supplier_id);
                    command.Parameters.AddWithValue("@order_date", model.order_date);
                    command.Parameters.AddWithValue("@status", model.status);
                    command.Parameters.AddWithValue("@total_amount", model.total_amount);

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        // DELETE
        public async Task<bool> DeletePurchaseOrder(int id)
        {
            const string query = @"
            DELETE FROM purchase_order_detail
            WHERE purchase_order_id = @id;

            DELETE FROM purchase_order
            WHERE purchase_order_id = @id;
            ";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@id", id);

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // GET ALL
        public async Task<List<PurchaseOrder>> GetAllPurchaseOrder()
        {
            const string query = @"
            SELECT
                po.*,
                s.supplier_name
            FROM purchase_order po
            LEFT JOIN master_supplier s
                ON po.supplier_id = s.supplier_id
            ";

            var response = new List<PurchaseOrder>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var purchaseOrder = new PurchaseOrder()
                            {
                                purchase_order_id = reader.GetInt32(reader.GetOrdinal("purchase_order_id")),
                                po_number = reader["po_number"].ToString(),
                                supplier_id = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                                order_date = reader.GetDateTime(reader.GetOrdinal("order_date")),
                                status = reader["status"].ToString(),
                                total_amount = reader.GetDecimal(reader.GetOrdinal("total_amount"))
                            };

                            purchaseOrder.Supplier = new Supplier
                            {
                                supplier_id = purchaseOrder.supplier_id,
                                supplier_name = reader["supplier_name"]?.ToString() ?? ""
                            };

                            response.Add(purchaseOrder);
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

        // GET BY ID
        public async Task<PurchaseOrder?> GetPurchaseOrderById(int id)
        {
            const string query = @"
                SELECT * FROM purchase_order
                WHERE purchase_order_id = @id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new PurchaseOrder()
                            {
                                purchase_order_id = reader.GetInt32(reader.GetOrdinal("purchase_order_id")),
                                po_number = reader["po_number"].ToString(),
                                supplier_id = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                                order_date = reader.GetDateTime(reader.GetOrdinal("order_date")),
                                status = reader["status"].ToString(),
                                total_amount = reader.GetDecimal(reader.GetOrdinal("total_amount"))
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw new Exception(msg);
            }

            return null;
        }
    }
}