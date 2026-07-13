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
            // Use MAX on the numeric portion so the generated number is always
            // strictly greater than every existing po_number, regardless of the
            // order rows were inserted or whether any gaps exist.
            const string query = @"
                SELECT MAX(CAST(SUBSTRING(po_number, 4, 10) AS BIGINT))
                FROM purchase_order
                WHERE po_number LIKE 'PO-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(query, connection);

            object? result = await command.ExecuteScalarAsync();

            long nextNumber = 1;
            if (result != null && result != DBNull.Value)
                nextNumber = Convert.ToInt64(result) + 1;

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
                    tax_percentage,
                    tax_amount,
                    total_amount,
                    transaction_name,
                    transaction_detail,
                    expected_date,
                    nomor_faktur_pajak,
                    created_at
                )
                OUTPUT INSERTED.purchase_order_id
                VALUES
                (
                    @po_number,
                    @supplier_id,
                    @order_date,
                    @status,
                    @tax_percentage,
                    @tax_amount,
                    @total_amount,
                    @transaction_name,
                    @transaction_detail,
                    @expected_date,
                    @nomor_faktur_pajak,
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
                    command.Parameters.AddWithValue("@tax_percentage", (object?)model.tax_percentage ?? DBNull.Value);
                    command.Parameters.AddWithValue("@tax_amount", (object?)model.tax_amount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@total_amount", (object?)model.total_amount ?? (object)0m);
                    command.Parameters.AddWithValue("@transaction_name", (object?)model.transaction_name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@transaction_detail", (object?)model.transaction_detail ?? DBNull.Value);
                    command.Parameters.AddWithValue("@expected_date", (object?)model.expected_date ?? DBNull.Value);
                    command.Parameters.AddWithValue("@nomor_faktur_pajak", (object?)model.nomor_faktur_pajak ?? DBNull.Value);

                    int insertedId =
                        Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );

                    return insertedId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InsertPurchaseOrder] Exception: {ex.Message}\n{ex.StackTrace}");
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
                    tax_percentage = @tax_percentage,
                    tax_amount = @tax_amount,
                    total_amount = @total_amount,
                    transaction_name = @transaction_name,
                    transaction_detail = @transaction_detail,
                    expected_date = @expected_date,
                    nomor_faktur_pajak = @nomor_faktur_pajak
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
                    command.Parameters.AddWithValue("@tax_percentage", (object?)model.tax_percentage ?? DBNull.Value);
                    command.Parameters.AddWithValue("@tax_amount", (object?)model.tax_amount ?? DBNull.Value);
                    command.Parameters.AddWithValue("@total_amount", (object?)model.total_amount ?? (object)0m);
                    command.Parameters.AddWithValue("@transaction_name", (object?)model.transaction_name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@transaction_detail", (object?)model.transaction_detail ?? DBNull.Value);
                    command.Parameters.AddWithValue("@expected_date", (object?)model.expected_date ?? DBNull.Value);
                    command.Parameters.AddWithValue("@nomor_faktur_pajak", (object?)model.nomor_faktur_pajak ?? DBNull.Value);

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
                                tax_percentage = reader["tax_percentage"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["tax_percentage"])
                                    : null,
                                tax_amount = reader["tax_amount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["tax_amount"])
                                    : null,
                                total_amount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                                transaction_name = reader["transaction_name"] != DBNull.Value
                                    ? reader["transaction_name"].ToString()
                                    : null,
                                transaction_detail = reader["transaction_detail"] != DBNull.Value
                                    ? reader["transaction_detail"].ToString()
                                    : null,
                                expected_date = reader["expected_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["expected_date"])
                                    : null,
                                nomor_faktur_pajak = reader["nomor_faktur_pajak"] != DBNull.Value
                                    ? reader["nomor_faktur_pajak"].ToString()
                                    : null
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
                                tax_percentage = reader["tax_percentage"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["tax_percentage"])
                                    : null,
                                tax_amount = reader["tax_amount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["tax_amount"])
                                    : null,
                                total_amount = reader.GetDecimal(reader.GetOrdinal("total_amount")),
                                transaction_name = reader["transaction_name"] != DBNull.Value
                                    ? reader["transaction_name"].ToString()
                                    : null,
                                transaction_detail = reader["transaction_detail"] != DBNull.Value
                                    ? reader["transaction_detail"].ToString()
                                    : null,
                                expected_date = reader["expected_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["expected_date"])
                                    : null,
                                nomor_faktur_pajak = reader["nomor_faktur_pajak"] != DBNull.Value
                                    ? reader["nomor_faktur_pajak"].ToString()
                                    : null
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