using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IGoodsReceiptRepo
    {
        Task<int> InsertGoodsReceipt(GoodsReceipt model);

        Task<List<GoodsReceipt>> GetAllGoodsReceipt();

        Task<bool> UpdateGoodsReceipt(GoodsReceipt model);

        Task<bool> DeleteGoodsReceipt(int id);
    }

    public class GoodsReceiptRepo : IGoodsReceiptRepo
    {
        private readonly string _connectionString;

        public GoodsReceiptRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        // INSERT
        public async Task<int> InsertGoodsReceipt(GoodsReceipt model)
        {
            const string query = @"
                INSERT INTO goods_receipt
                (
                    purchase_order_id,
                    receipt_number,
                    receipt_date,
                    received_by,
                    status
                )
                VALUES
                (
                    @purchase_order_id,
                    @receipt_number,
                    @receipt_date,
                    @received_by,
                    @status
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@purchase_order_id", model.purchase_order_id);
                    command.Parameters.AddWithValue("@receipt_number", model.receipt_number);
                    command.Parameters.AddWithValue("@receipt_date", model.receipt_date);
                    command.Parameters.AddWithValue("@received_by", model.received_by);
                    command.Parameters.AddWithValue("@status", model.status);

                    int goodsReceiptId =
                        (int)await command.ExecuteScalarAsync();

                    return goodsReceiptId;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // UPDATE
        public async Task<bool> UpdateGoodsReceipt(GoodsReceipt model)
        {
            const string query = @"
                UPDATE goods_receipt
                SET
                    receipt_number = @receipt_number,
                    receipt_date = @receipt_date,
                    received_by = @received_by,
                    status = @status
                WHERE goods_receipt_id = @goods_receipt_id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@goods_receipt_id", model.goods_receipt_id);
                    command.Parameters.AddWithValue("@receipt_number", model.receipt_number);
                    command.Parameters.AddWithValue("@receipt_date", model.receipt_date);
                    command.Parameters.AddWithValue("@received_by", model.received_by);
                    command.Parameters.AddWithValue("@status", model.status);

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
        public async Task<bool> DeleteGoodsReceipt(int id)
        {
            const string query = @"
                DELETE FROM goods_receipt
                WHERE goods_receipt_id = @id";

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
        public async Task<List<GoodsReceipt>> GetAllGoodsReceipt()
        {
            const string query = @"
            SELECT
                gr.*,
                po.supplier_id,
                po.total_amount,
                s.supplier_name
            FROM goods_receipt gr
            INNER JOIN purchase_order po
                ON gr.purchase_order_id =
                po.purchase_order_id
            INNER JOIN master_supplier s
                ON po.supplier_id =
                s.supplier_id";

            var response = new List<GoodsReceipt>();

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
                            var receipt = new GoodsReceipt()
                            {
                                goods_receipt_id =
                                    reader["goods_receipt_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["goods_receipt_id"])
                                    : 0,

                                purchase_order_id =
                                    reader["purchase_order_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["purchase_order_id"])
                                    : 0,

                                receipt_number =
                                    reader["receipt_number"]?.ToString() ?? "",

                                receipt_date =
                                    reader["receipt_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["receipt_date"])
                                    : DateTime.Now,

                                received_by =
                                    reader["received_by"]?.ToString() ?? "",

                                status =
                                    reader["status"]?.ToString() ?? "",

                                supplier_id =
                                    reader["supplier_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["supplier_id"])
                                    : 0,

                                supplier_name =
                                    reader["supplier_name"]?.ToString() ?? "",

                                total_amount =
                                    reader["total_amount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["total_amount"])
                                    : 0,

                            };

                            response.Add(receipt);
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
    }
}