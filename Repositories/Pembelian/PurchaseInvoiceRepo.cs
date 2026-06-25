using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseInvoiceRepo
    {
        Task<string> GenerateInvoiceNumber();

        Task<int> InsertPurchaseInvoice(
            PurchaseInvoice model
        );

        Task<List<PurchaseInvoice>>
            GetAllPurchaseInvoice();

        Task<bool> IsInvoiceExist(
            int goodsReceiptId
        );

        Task<PurchaseInvoice?>
            GetPurchaseInvoiceById(
                int id
            );

        Task<bool> UpdatePurchaseInvoice(
            PurchaseInvoice model
        );

        Task<bool> DeletePurchaseInvoice(
            int id
        );
    }

    public class PurchaseInvoiceRepo : IPurchaseInvoiceRepo
    {
        private readonly string _connectionString;

        public PurchaseInvoiceRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // GENERATE INVOICE NUMBER
        public async Task<string> GenerateInvoiceNumber()
        {
            const string query = @"
                SELECT TOP 1 invoice_number
                FROM purchase_invoice
                ORDER BY purchase_invoice_id DESC";

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
                string lastInv =
                    result.ToString() ?? "INV000000";

                string numericPart =
                    lastInv.Replace("INV", "");

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"INV{nextNumber:D6}";
        }

        // INSERT
        public async Task<int> InsertPurchaseInvoice(
            PurchaseInvoice model
        )
        {
            const string query = @"
                INSERT INTO purchase_invoice
                (
                    goods_receipt_id,
                    invoice_number,
                    invoice_date,
                    supplier_id,
                    total_amount,
                    status,
                    created_at
                )
                VALUES
                (
                    @goods_receipt_id,
                    @invoice_number,
                    @invoice_date,
                    @supplier_id,
                    @total_amount,
                    @status,
                    GETDATE()
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(_connectionString))

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@goods_receipt_id",
                        model.goods_receipt_id
                    );

                    command.Parameters.AddWithValue(
                        "@invoice_number",
                        model.invoice_number
                    );

                    command.Parameters.AddWithValue(
                        "@invoice_date",
                        model.invoice_date
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_id",
                        model.supplier_id
                    );

                    command.Parameters.AddWithValue(
                        "@total_amount",
                        model.total_amount
                    );

                    command.Parameters.AddWithValue(
                        "@status",
                        model.status
                    );

                    int purchaseInvoiceId =
                        (int)await command.ExecuteScalarAsync();

                    return purchaseInvoiceId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> IsInvoiceExist(
            int goodsReceiptId
        )
        {
            const string query = @"
                SELECT COUNT(*)
                FROM purchase_invoice
                WHERE goods_receipt_id =
                    @goods_receipt_id";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))

            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@goods_receipt_id",
                    goodsReceiptId
                );

                int count =
                    Convert.ToInt32(
                        await command.ExecuteScalarAsync()
                    );

                return count > 0;
            }
        }

        public async Task<PurchaseInvoice?>
            GetPurchaseInvoiceById(
                int id
            )
        {
            const string query = @"
                SELECT *
                FROM purchase_invoice
                WHERE purchase_invoice_id = @id";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))

            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@id",
                    id
                );

                Console.WriteLine(query);

                using (SqlDataReader reader =
                    await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new PurchaseInvoice
                        {
                            purchase_invoice_id =
                                Convert.ToInt32(
                                    reader["purchase_invoice_id"]
                                ),

                            goods_receipt_id =
                                Convert.ToInt32(
                                    reader["goods_receipt_id"]
                                ),

                            invoice_number =
                                reader["invoice_number"]
                                    ?.ToString() ?? "",

                            supplier_id =
                                Convert.ToInt32(
                                    reader["supplier_id"]
                                ),

                            total_amount =
                                Convert.ToDecimal(
                                    reader["total_amount"]
                                ),

                            status =
                                reader["status"]
                                    ?.ToString() ?? ""
                        };
                    }
                }
            }

            return null;
        }

        public async Task<bool>
            UpdatePurchaseInvoice(
                PurchaseInvoice model
            )
        {
            const string query = @"
                UPDATE purchase_invoice
                SET
                    status = @status
                WHERE purchase_invoice_id =
                    @purchase_invoice_id";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))

            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@purchase_invoice_id",
                    model.purchase_invoice_id
                );

                command.Parameters.AddWithValue(
                    "@status",
                    model.status
                );

                int result =
                    await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }

        public async Task<bool>
    DeletePurchaseInvoice(
        int id
    )
{
    const string query = @"
        DELETE FROM purchase_invoice
        WHERE purchase_invoice_id = @id";

    using (SqlConnection connection =
        new SqlConnection(_connectionString))

    using (SqlCommand command =
        new SqlCommand(query, connection))
    {
        await connection.OpenAsync();

        command.Parameters.AddWithValue(
            "@id",
            id
        );

        int result =
            await command.ExecuteNonQueryAsync();

        return result > 0;
    }
}

        // GET ALL
        public async Task<List<PurchaseInvoice>>
            GetAllPurchaseInvoice()
        {
        const string query = @"
        SELECT
            pi.*,
            ms.supplier_name,

            ISNULL(
                (
                    SELECT SUM(pdp.amount)
                    FROM purchase_down_payment pdp
                    WHERE pdp.purchase_order_id =
                        gr.purchase_order_id
                ),
                0
            ) AS dp_paid,

            ISNULL(
                pi.total_amount
                -
                (
                    SELECT ISNULL(
                        SUM(pdp.amount),
                        0
                    )
                    FROM purchase_down_payment pdp
                    WHERE pdp.purchase_order_id =
                        gr.purchase_order_id
                )
                -
                (
                    SELECT ISNULL(
                        SUM(pp.amount),
                        0
                    )
                    FROM purchase_payment pp
                    WHERE pp.purchase_invoice_id =
                        pi.purchase_invoice_id
                ),
                pi.total_amount
            ) AS outstanding_amount

        FROM purchase_invoice pi

        LEFT JOIN goods_receipt gr
            ON pi.goods_receipt_id =
            gr.goods_receipt_id

        LEFT JOIN master_supplier ms
            ON pi.supplier_id =
            ms.supplier_id

        ORDER BY pi.purchase_invoice_id DESC";

            var response =
                new List<PurchaseInvoice>();

            try
            {
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
                            response.Add(
                                new PurchaseInvoice
                                {
                                    purchase_invoice_id =
                                        Convert.ToInt32(
                                            reader["purchase_invoice_id"]
                                            
                                        ),

                                    goods_receipt_id =
                                        Convert.ToInt32(
                                            reader["goods_receipt_id"]
                                        ),

                                    invoice_number =
                                        reader["invoice_number"]
                                            ?.ToString() ?? "",

                                    invoice_date =
                                        Convert.ToDateTime(
                                            reader["invoice_date"]
                                        ),

                                    supplier_id =
                                        Convert.ToInt32(
                                            reader["supplier_id"]
                                        ),

                                    total_amount =
                                        Convert.ToDecimal(
                                            reader["total_amount"]
                                        ),

                                    status =
                                        reader["status"]
                                            ?.ToString() ?? "",

                                    created_at =
                                        Convert.ToDateTime(
                                            reader["created_at"]
                                        ),

                                    supplier_name =
                                     reader["supplier_name"]?.ToString() ?? "",              

                                     dp_paid =
                                        reader["dp_paid"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(
                                                reader["dp_paid"]
                                            ),

                                    outstanding_amount =
                                        reader["outstanding_amount"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(
                                                reader["outstanding_amount"]
                                            ),
                                }
                            );
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