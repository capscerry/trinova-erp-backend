using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
public interface IPurchasePaymentRepo
{
    Task<string> GeneratePaymentNumber();

    Task<int> InsertPurchasePayment(
    PurchasePayment model
    );

    Task<List<PurchasePayment>>
        GetAllPurchasePayment();

    Task<bool> UpdatePurchasePayment(
        int id,
        PurchasePayment model
    );

    Task<bool> DeletePurchasePayment(
        int id
    );

    /// <summary>
    /// Returns the purchase_invoice_id that a payment belongs to,
    /// or null if the payment does not exist.
    /// </summary>
    Task<int?> GetInvoiceIdByPaymentId(int paymentId);

    /// <summary>
    /// Returns true when a non-deleted payment already exists for the same
    /// invoice, date, and amount — used to prevent accidental double-submits.
    /// Pass excludePaymentId when updating an existing record so the record
    /// being edited is not compared against itself.
    /// </summary>
    Task<bool> IsDuplicatePayment(
        int purchaseInvoiceId,
        DateTime paymentDate,
        decimal amount,
        int? excludePaymentId = null
    );

    /// <summary>
    /// Returns the sum of all existing payment amounts for the given invoice,
    /// optionally excluding one payment row (pass the id of the row being
    /// edited so it is not counted against itself).
    /// Returns 0 when no payments exist yet.
    /// </summary>
    Task<decimal> GetTotalPaidByInvoice(
        int purchaseInvoiceId,
        int? excludePaymentId = null
    );
}

public class PurchasePaymentRepo
    : IPurchasePaymentRepo
{
    private readonly string _connectionString;

    public PurchasePaymentRepo(
        IOptionsSnapshot<DatabaseConnection> options
    )
    {
        _connectionString =
            options.Value.SQLServer
            ?? throw new InvalidOperationException(
                "Database connection string is not configured."
            );
    }

    // GENERATE PAYMENT NUMBER
    public async Task<string> GeneratePaymentNumber()
    {
        // Query only rows that already follow the canonical PAY-NNNNNNNNNN
        // format so that leftover legacy numbers can never corrupt the counter.
        const string query = @"
            SELECT TOP 1 payment_number
            FROM purchase_payment
            WHERE payment_number LIKE 'PAY-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
            ORDER BY purchase_payment_id DESC";

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
            string lastPay =
                result.ToString() ?? "PAY-0000000000";

            // Strip the "PAY-" prefix (always 4 chars) before parsing
            string numericPart = lastPay.Substring(4);

            if (int.TryParse(numericPart, out int parsed))
                nextNumber = parsed + 1;
        }

        return $"PAY-{nextNumber:D10}";
    }

    // INSERT
    public async Task<int>
        InsertPurchasePayment(
            PurchasePayment model
        )
    {
        const string query = @"
        INSERT INTO purchase_payment
        (
            payment_number,
            purchase_invoice_id,
            payment_date,
            amount,
            payment_method,
            status,
            notes,
            created_at
        )
        VALUES
        (
            @payment_number,
            @purchase_invoice_id,
            @payment_date,
            @amount,
            @payment_method,
            @status,
            @notes,
            DATEADD(HOUR, 7, GETUTCDATE())
        );

        SELECT CAST(
            SCOPE_IDENTITY() AS INT
        );";

        using (SqlConnection connection =
            new SqlConnection(
                _connectionString
            ))

        using (SqlCommand command =
            new SqlCommand(
                query,
                connection
            ))
        {
            await connection.OpenAsync();

            command.Parameters.AddWithValue(
                "@payment_number",
                model.payment_number
            );

            command.Parameters.AddWithValue(
                "@purchase_invoice_id",
                model.purchase_invoice_id
            );

            command.Parameters.AddWithValue(
                "@payment_date",
                model.payment_date
            );

            command.Parameters.AddWithValue(
                "@amount",
                model.amount
            );

            command.Parameters.AddWithValue(
                "@payment_method",
                model.payment_method ?? ""
            );

            command.Parameters.AddWithValue(
                "@status",
                model.status ?? ""
            );

            command.Parameters.AddWithValue(
                "@notes",
                model.notes ?? ""
            );

            int paymentId =
                (int)await command
                    .ExecuteScalarAsync();

            return paymentId;
        }
    }

    // GET ALL
    public async Task<List<PurchasePayment>>
        GetAllPurchasePayment()
    {
        const string query = @"
        SELECT
            pp.*,
            pi.invoice_number,
            ms.supplier_name,
            po.transaction_name,
            po.transaction_detail

        FROM purchase_payment pp

        LEFT JOIN purchase_invoice pi
            ON pp.purchase_invoice_id =
            pi.purchase_invoice_id

        LEFT JOIN goods_receipt gr
            ON pi.goods_receipt_id =
            gr.goods_receipt_id

        LEFT JOIN purchase_order po
            ON gr.purchase_order_id =
            po.purchase_order_id

        LEFT JOIN master_supplier ms
            ON pi.supplier_id =
            ms.supplier_id

        ORDER BY
            pp.purchase_payment_id DESC";

        var response =
            new List<PurchasePayment>();

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
                        new PurchasePayment
                        {
                            purchase_payment_id =
                                Convert.ToInt32(
                                    reader["purchase_payment_id"]
                                ),

                            payment_number =
                                reader["payment_number"]
                                    ?.ToString() ?? "",

                            purchase_invoice_id =
                                Convert.ToInt32(
                                    reader["purchase_invoice_id"]
                                ),

                            payment_date =
                                Convert.ToDateTime(
                                    reader["payment_date"]
                                ),

                            amount =
                                Convert.ToDecimal(
                                    reader["amount"]
                                ),

                            payment_method =
                                reader["payment_method"]
                                    ?.ToString(),

                            status =
                                reader["status"]
                                    ?.ToString(),

                            notes =
                                reader["notes"]
                                    ?.ToString(),

                            invoice_number =
                                reader["invoice_number"]
                                    ?.ToString(),

                            supplier_name =
                                reader["supplier_name"]
                                    ?.ToString(),

                            transaction_name =
                                reader["transaction_name"] == DBNull.Value
                                    ? null
                                    : reader["transaction_name"]?.ToString(),

                            transaction_detail =
                                reader["transaction_detail"] == DBNull.Value
                                    ? null
                                    : reader["transaction_detail"]?.ToString()
                        }
                    );
                }
            }
        }

        return response;
    }

    // UPDATE
    public async Task<bool> UpdatePurchasePayment(
        int id,
        PurchasePayment model
    )
    {
        const string query = @"
            UPDATE purchase_payment
            SET
                payment_date   = @payment_date,
                amount         = @amount,
                payment_method = @payment_method,
                status         = @status,
                notes          = @notes
            WHERE purchase_payment_id = @id";

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(query, connection);

        await connection.OpenAsync();

        command.Parameters.AddWithValue("@id",             id);
        command.Parameters.AddWithValue("@payment_date",   model.payment_date);
        command.Parameters.AddWithValue("@amount",         model.amount);
        command.Parameters.AddWithValue("@payment_method", model.payment_method ?? "");
        command.Parameters.AddWithValue("@status",         model.status ?? "");
        command.Parameters.AddWithValue("@notes",          model.notes ?? "");

        int result = await command.ExecuteNonQueryAsync();
        return result > 0;
    }

    // DELETE
    public async Task<bool>
        DeletePurchasePayment(
            int id
        )
    {
        const string query = @"
            DELETE FROM purchase_payment
            WHERE purchase_payment_id = @id";

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
    // GET INVOICE ID BY PAYMENT ID
    public async Task<int?> GetInvoiceIdByPaymentId(int paymentId)
    {
        const string query = @"
            SELECT purchase_invoice_id
            FROM purchase_payment
            WHERE purchase_payment_id = @id";

        using SqlConnection connection = new SqlConnection(_connectionString);
        using SqlCommand command = new SqlCommand(query, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@id", paymentId);

        object? result = await command.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value) return null;
        return Convert.ToInt32(result);
    }

    // GET TOTAL PAID BY INVOICE
    // Returns the sum of all payment amounts already recorded for the given
    // invoice. Pass excludePaymentId when editing an existing payment so the
    // row being updated is not counted against itself.
    // Returns 0 when no other payments exist.
    public async Task<decimal> GetTotalPaidByInvoice(
        int purchaseInvoiceId,
        int? excludePaymentId = null
    )
    {
        const string query = @"
            SELECT ISNULL(SUM(amount), 0)
            FROM purchase_payment
            WHERE purchase_invoice_id = @purchase_invoice_id
              AND (@exclude_id IS NULL
                   OR purchase_payment_id <> @exclude_id)";

        using SqlConnection connection = new SqlConnection(_connectionString);
        using SqlCommand command = new SqlCommand(query, connection);
        await connection.OpenAsync();

        command.Parameters.AddWithValue("@purchase_invoice_id", purchaseInvoiceId);
        command.Parameters.AddWithValue(
            "@exclude_id",
            excludePaymentId.HasValue ? (object)excludePaymentId.Value : DBNull.Value
        );

        object? result = await command.ExecuteScalarAsync();
        return result == null || result == DBNull.Value
            ? 0m
            : Convert.ToDecimal(result);
    }

    // IS DUPLICATE PAYMENT
    // Checks whether a payment with the same invoice, date, and amount
    // already exists. The date comparison ignores the time component so
    // that two payments submitted seconds apart on the same day are still
    // caught. Pass excludePaymentId when editing an existing record so the
    // record being updated is not flagged against itself.
    public async Task<bool> IsDuplicatePayment(
        int purchaseInvoiceId,
        DateTime paymentDate,
        decimal amount,
        int? excludePaymentId = null
    )
    {
        const string query = @"
            SELECT COUNT(*)
            FROM purchase_payment
            WHERE purchase_invoice_id = @purchase_invoice_id
              AND CAST(payment_date AS DATE) = CAST(@payment_date AS DATE)
              AND amount = @amount
              AND (@exclude_id IS NULL
                   OR purchase_payment_id <> @exclude_id)";

        using SqlConnection connection = new SqlConnection(_connectionString);
        using SqlCommand command = new SqlCommand(query, connection);
        await connection.OpenAsync();

        command.Parameters.AddWithValue("@purchase_invoice_id", purchaseInvoiceId);
        command.Parameters.AddWithValue("@payment_date",        paymentDate);
        command.Parameters.AddWithValue("@amount",              amount);
        command.Parameters.AddWithValue(
            "@exclude_id",
            excludePaymentId.HasValue ? (object)excludePaymentId.Value : DBNull.Value
        );

        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }
}

}