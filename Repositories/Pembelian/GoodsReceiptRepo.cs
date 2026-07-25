using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IGoodsReceiptRepo
    {
        Task<string> GenerateGRNumber();

        Task<int> InsertGoodsReceipt(GoodsReceipt model);

        Task<List<GoodsReceipt>> GetAllGoodsReceipt();

        Task<List<GoodsReceipt>> GetAllWithoutInvoice();

        /// <summary>
        /// Returns only Goods Receipts that have at least one detail line
        /// with remaining_qty &gt; 0.  Used by the Purchase Return creation
        /// modal so exhausted GRs are never shown.
        /// </summary>
        Task<List<GoodsReceipt>> GetAllAvailableForReturn();

        Task<GoodsReceipt?> GetGoodsReceiptById(int id);

        Task<bool> UpdateGoodsReceipt(GoodsReceipt model);

        Task<bool> UpdateGoodsReceiptStatus(int id, string status);

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

        // GENERATE GR NUMBER
        public async Task<string> GenerateGRNumber()
        {
            // Query only rows that already follow the canonical GR-NNNNNNNNNN
            // format so that leftover legacy numbers can never corrupt the counter.
            const string query = @"
                SELECT TOP 1 receipt_number
                FROM goods_receipt
                WHERE receipt_number LIKE 'GR-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                ORDER BY goods_receipt_id DESC";

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
                string lastGr =
                    result.ToString() ?? "GR-0000000000";

                // Strip the "GR-" prefix (always 3 chars) before parsing
                string numericPart = lastGr.Substring(3);

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"GR-{nextNumber:D10}";
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

        // UPDATE STATUS ONLY
        public async Task<bool> UpdateGoodsReceiptStatus(int id, string status)
        {
            const string query = @"
                UPDATE goods_receipt
                SET status = @status
                WHERE goods_receipt_id = @goods_receipt_id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@goods_receipt_id", id);
                    command.Parameters.AddWithValue("@status", status);

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
                po.po_number,
                po.transaction_name,
                po.transaction_detail,
                po.nomor_faktur_pajak,
                s.supplier_name
            FROM goods_receipt gr
            INNER JOIN purchase_order po
                ON gr.purchase_order_id =
                po.purchase_order_id
            INNER JOIN master_supplier s
                ON po.supplier_id =
                s.supplier_id
            ORDER BY gr.goods_receipt_id DESC";

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

                                po_number =
                                    reader["po_number"]?.ToString() ?? "",

                                transaction_name =
                                    reader["transaction_name"] != DBNull.Value
                                    ? reader["transaction_name"]?.ToString()
                                    : null,

                                transaction_detail =
                                    reader["transaction_detail"] != DBNull.Value
                                    ? reader["transaction_detail"]?.ToString()
                                    : null,

                                nomor_faktur_pajak =
                                    reader["nomor_faktur_pajak"] != DBNull.Value
                                    ? reader["nomor_faktur_pajak"]?.ToString()
                                    : null,

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
        // GET ALL AVAILABLE FOR RETURN
        // Returns GRs that have at least one detail line where remaining_qty > 0.
        // This is the only list the Purchase Return creation modal should load.
        public async Task<List<GoodsReceipt>> GetAllAvailableForReturn()
        {
            const string query = @"
            SELECT DISTINCT
                gr.goods_receipt_id,
                gr.purchase_order_id,
                gr.receipt_number,
                gr.receipt_date,
                gr.received_by,
                gr.status,
                gr.created_at,
                po.supplier_id,
                po.total_amount,
                po.po_number,
                po.transaction_name,
                po.transaction_detail,
                po.nomor_faktur_pajak,
                s.supplier_name
            FROM goods_receipt gr
            INNER JOIN purchase_order po
                ON gr.purchase_order_id = po.purchase_order_id
            INNER JOIN master_supplier s
                ON po.supplier_id = s.supplier_id
            -- Only include GRs that have at least one returnable detail line
            WHERE EXISTS (
                SELECT 1
                FROM goods_receipt_detail grd
                WHERE grd.goods_receipt_id = gr.goods_receipt_id
                  AND grd.remaining_qty > 0
            )
            ORDER BY gr.goods_receipt_id DESC";

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
                            response.Add(new GoodsReceipt
                            {
                                goods_receipt_id =
                                    reader["goods_receipt_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["goods_receipt_id"]) : 0,

                                purchase_order_id =
                                    reader["purchase_order_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["purchase_order_id"]) : 0,

                                receipt_number =
                                    reader["receipt_number"]?.ToString() ?? "",

                                receipt_date =
                                    reader["receipt_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["receipt_date"]) : DateTime.Now,

                                received_by =
                                    reader["received_by"]?.ToString() ?? "",

                                status =
                                    reader["status"]?.ToString() ?? "",

                                supplier_id =
                                    reader["supplier_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["supplier_id"]) : 0,

                                supplier_name =
                                    reader["supplier_name"]?.ToString() ?? "",

                                total_amount =
                                    reader["total_amount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["total_amount"]) : 0,

                                po_number =
                                    reader["po_number"]?.ToString() ?? "",

                                transaction_name =
                                    reader["transaction_name"] != DBNull.Value
                                    ? reader["transaction_name"]?.ToString() : null,

                                transaction_detail =
                                    reader["transaction_detail"] != DBNull.Value
                                    ? reader["transaction_detail"]?.ToString() : null,

                                nomor_faktur_pajak =
                                    reader["nomor_faktur_pajak"] != DBNull.Value
                                    ? reader["nomor_faktur_pajak"]?.ToString() : null,
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return response;
        }

        // GET BY ID
        public async Task<GoodsReceipt?> GetGoodsReceiptById(int id)
        {
            const string query = @"
            SELECT
                gr.*,
                po.supplier_id,
                po.total_amount,
                po.po_number,
                po.nomor_faktur_pajak,
                s.supplier_name
            FROM goods_receipt gr
            INNER JOIN purchase_order po
                ON gr.purchase_order_id = po.purchase_order_id
            INNER JOIN master_supplier s
                ON po.supplier_id = s.supplier_id
            WHERE gr.goods_receipt_id = @id";

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
                            return new GoodsReceipt
                            {
                                goods_receipt_id =
                                    Convert.ToInt32(reader["goods_receipt_id"]),
                                purchase_order_id =
                                    Convert.ToInt32(reader["purchase_order_id"]),
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
                                    Convert.ToInt32(reader["supplier_id"]),
                                supplier_name =
                                    reader["supplier_name"]?.ToString() ?? "",
                                total_amount =
                                    reader["total_amount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["total_amount"]) : 0,
                                po_number =
                                    reader["po_number"]?.ToString() ?? "",

                                nomor_faktur_pajak =
                                    reader["nomor_faktur_pajak"] != DBNull.Value
                                    ? reader["nomor_faktur_pajak"]?.ToString()
                                    : null
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return null;
        }

        // GET ALL WITHOUT INVOICE        // Returns only GR records that do NOT yet have a purchase_invoice
        public async Task<List<GoodsReceipt>> GetAllWithoutInvoice()
        {
            const string query = @"
            SELECT
                gr.*,
                po.supplier_id,
                po.total_amount,
                po.po_number,
                po.transaction_name,
                po.transaction_detail,
                po.nomor_faktur_pajak,
                s.supplier_name
            FROM goods_receipt gr
            INNER JOIN purchase_order po
                ON gr.purchase_order_id =
                po.purchase_order_id
            INNER JOIN master_supplier s
                ON po.supplier_id =
                s.supplier_id
            LEFT JOIN purchase_invoice pi
                ON gr.goods_receipt_id =
                pi.goods_receipt_id
            WHERE pi.goods_receipt_id IS NULL
            ORDER BY gr.goods_receipt_id DESC";

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
                            response.Add(new GoodsReceipt()
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

                                po_number =
                                    reader["po_number"]?.ToString() ?? "",

                                transaction_name =
                                    reader["transaction_name"] != DBNull.Value
                                    ? reader["transaction_name"]?.ToString()
                                    : null,

                                transaction_detail =
                                    reader["transaction_detail"] != DBNull.Value
                                    ? reader["transaction_detail"]?.ToString()
                                    : null,

                                nomor_faktur_pajak =
                                    reader["nomor_faktur_pajak"] != DBNull.Value
                                    ? reader["nomor_faktur_pajak"]?.ToString()
                                    : null,
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return response;
        }
    }
}