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

    Task<bool> DeletePurchasePayment(
        int id
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
        const string query = @"
            SELECT TOP 1 payment_number
            FROM purchase_payment
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
                result.ToString() ?? "PAY000000";

            string numericPart =
                lastPay.Replace("PAY", "");

            if (int.TryParse(numericPart, out int parsed))
                nextNumber = parsed + 1;
        }

        return $"PAY{nextNumber:D6}";
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
            GETDATE()
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
}

}
