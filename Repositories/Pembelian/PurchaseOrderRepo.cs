using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseOrderRepo
    {
        Task<bool> InsertPurchaseOrder(PurchaseOrder model);

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

        // INSERT
        public async Task<bool> InsertPurchaseOrder(PurchaseOrder model)
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

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
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
            catch (Exception)
            {
                return false;
            }
        }

        // DELETE
        public async Task<bool> DeletePurchaseOrder(int id)
        {
            const string query = @"
                DELETE FROM purchase_order
                WHERE purchase_order_id = @id";

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
            catch (Exception)
            {
                return false;
            }
        }

        // GET ALL
        public async Task<List<PurchaseOrder>> GetAllPurchaseOrder()
        {
            const string query = @"SELECT * FROM purchase_order";

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