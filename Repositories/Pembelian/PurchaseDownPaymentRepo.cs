using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseDownPaymentRepo
    {
        Task<string> GenerateDPNumber();

        Task<int> InsertPurchaseDownPayment(
            PurchaseDownPayment model
        );

        Task<List<PurchaseDownPayment>>
            GetAllPurchaseDownPayment(int? id = null);

        Task<PurchaseDownPayment?>
            GetPurchaseDownPaymentById(
                int id
            );

        Task<bool> UpdatePurchaseDownPayment(
            PurchaseDownPayment model
        );

        Task<bool> DeletePurchaseDownPayment(
            int id
        );

        /// <summary>
        /// Returns true when a Down Payment already exists for the given
        /// purchase_order_id.  Used to prevent duplicate DP creation.
        /// </summary>
        Task<bool> IsDownPaymentExist(int purchaseOrderId);
    }

    public class PurchaseDownPaymentRepo
        : IPurchaseDownPaymentRepo
    {
        private readonly string _connectionString;

        public PurchaseDownPaymentRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        public async Task<string> GenerateDPNumber()
        {
            // Use MAX on the numeric portion so the generated number is always
            // strictly greater than every existing dp_number, preventing
            // duplicate key errors from ghost rows.
            const string query = @"
                SELECT MAX(CAST(SUBSTRING(dp_number, 4, 10) AS BIGINT))
                FROM purchase_down_payment
                WHERE dp_number LIKE 'DP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(query, connection);

            object? result = await command.ExecuteScalarAsync();

            long nextNumber = 1;
            if (result != null && result != DBNull.Value)
                nextNumber = Convert.ToInt64(result) + 1;

            return $"DP-{nextNumber:D10}";
        }

        public async Task<int>
            InsertPurchaseDownPayment(
                PurchaseDownPayment model
            )
        {
            const string query = @"
                INSERT INTO purchase_down_payment
                (
                    dp_number,
                    purchase_order_id,
                    supplier_id,
                    payment_date,
                    payment_type,
                    amount,
                    status,
                    notes,
                    created_at
                )
                OUTPUT INSERTED.purchase_down_payment_id
                VALUES
                (
                    @dp_number,
                    @purchase_order_id,
                    @supplier_id,
                    @payment_date,
                    @payment_type,
                    @amount,
                    @status,
                    @notes,
                    DATEADD(HOUR, 7, GETUTCDATE())
                )";

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@dp_number",
                        model.dp_number
                    );

                    command.Parameters.AddWithValue(
                        "@purchase_order_id",
                        model.purchase_order_id
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_id",
                        model.supplier_id
                    );

                    command.Parameters.AddWithValue(
                        "@payment_date",
                        model.payment_date ?? (object)DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@payment_type",
                        model.payment_type ?? (object)DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@amount",
                        model.amount ?? 0
                    );

                    command.Parameters.AddWithValue(
                        "@status",
                        model.status ?? (object)DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@notes",
                        model.notes ?? (object)DBNull.Value
                    );

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

        // GET ALL (atau satu baris saja kalau `id` diisi -- dipakai juga oleh
        // GetPurchaseDownPaymentById, yang sebelumnya cuma stub
        // NotImplementedException).
        public async Task<List<PurchaseDownPayment>>
            GetAllPurchaseDownPayment(int? id = null)
        {
            string query = @"
            SELECT
                pdp.*,
                po.po_number,
                po.total_amount AS po_total,
                po.transaction_name,
                po.transaction_detail,
                s.supplier_name
            FROM purchase_down_payment pdp
            LEFT JOIN purchase_order po
                ON pdp.purchase_order_id = po.purchase_order_id
            LEFT JOIN master_supplier s
                ON pdp.supplier_id = s.supplier_id
            " + (id.HasValue ? "WHERE pdp.purchase_down_payment_id = @id" : "");

            var response =
                new List<PurchaseDownPayment>();

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    if (id.HasValue)
                        command.Parameters.AddWithValue("@id", id.Value);

                    await connection.OpenAsync();

                    using (
                        SqlDataReader reader =
                        await command.ExecuteReaderAsync()
                    )
                    {
                        while (
                            await reader.ReadAsync()
                        )
                        {
                            var pdp =
                                new PurchaseDownPayment()
                                {
                                    purchase_down_payment_id =
                                        reader.GetInt32(
                                            reader.GetOrdinal(
                                                "purchase_down_payment_id"
                                            )
                                        ),

                                    dp_number =
                                        reader["dp_number"]
                                        ?.ToString()
                                        ?? "",

                                    purchase_order_id =
                                        reader.GetInt32(
                                            reader.GetOrdinal(
                                                "purchase_order_id"
                                            )
                                        ),

                                    supplier_id =
                                        reader.GetInt32(
                                            reader.GetOrdinal(
                                                "supplier_id"
                                            )
                                        ),

                                    payment_type =
                                        reader["payment_type"]
                                        ?.ToString(),

                                    status =
                                        reader["status"]
                                        ?.ToString(),

                                    payment_date =
                                        reader["payment_date"] == DBNull.Value
                                            ? null
                                            : reader.GetDateTime(
                                                reader.GetOrdinal(
                                                    "payment_date"
                                                )
                                            ),

                                    amount =
                                        reader["amount"] == DBNull.Value
                                            ? null
                                            : reader.GetDecimal(
                                                reader.GetOrdinal(
                                                    "amount"
                                                )
                                            ),

                                    notes =
                                        reader["notes"]
                                        ?.ToString(),

                                    created_at =
                                        reader["created_at"] == DBNull.Value
                                            ? null
                                            : reader.GetDateTime(
                                                reader.GetOrdinal(
                                                    "created_at"
                                                )
                                            ),

                                    supplier_name =
                                        reader["supplier_name"]
                                        ?.ToString(),

                                    po_number =
                                        reader["po_number"]
                                        ?.ToString()
                                        ,
                                    po_total =
                                        reader["po_total"] == DBNull.Value
                                            ? null
                                            : reader.GetDecimal(
                                                reader.GetOrdinal(
                                                    "po_total"
                                                )
                                            ),

                                    transaction_name =
                                        reader["transaction_name"] == DBNull.Value
                                            ? null
                                            : reader["transaction_name"]?.ToString(),

                                    transaction_detail =
                                        reader["transaction_detail"] == DBNull.Value
                                            ? null
                                            : reader["transaction_detail"]?.ToString()
                                };

                            response.Add(pdp);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    ex.Message
                );
            }

            return response;
        }

        public async Task<PurchaseDownPayment?>
            GetPurchaseDownPaymentById(
                int id
            )
        {
            var results = await GetAllPurchaseDownPayment(id);
            return results.FirstOrDefault();
        }

        public async Task<bool>
            UpdatePurchaseDownPayment(
                PurchaseDownPayment model
            )
        {
            throw new NotImplementedException();
        }

        public async Task<bool>
            IsDownPaymentExist(int purchaseOrderId)
        {
            const string query = @"
                SELECT COUNT(*)
                FROM purchase_down_payment
                WHERE purchase_order_id = @purchase_order_id";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using SqlCommand command =
                new SqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@purchase_order_id", purchaseOrderId);

            int count =
                Convert.ToInt32(
                    await command.ExecuteScalarAsync());

            return count > 0;
        }

        public async Task<bool>
            DeletePurchaseDownPayment(
                int id
            )
        {
            const string query = @"
                DELETE FROM purchase_down_payment
                WHERE purchase_down_payment_id = @id";

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                await connection.OpenAsync();

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", id);

                int rows = await command.ExecuteNonQueryAsync();

                return rows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}