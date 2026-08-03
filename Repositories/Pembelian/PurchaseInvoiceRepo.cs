using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    // ── Helper ────────────────────────────────────────────────────────────────
    // Safe column reader: returns null instead of throwing IndexOutOfRangeException
    // when a column is absent from the result set (e.g. before a migration runs).
    internal static class DataReaderExtensions
    {
        internal static string? SafeGetString(this SqlDataReader reader, string column)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
    // ─────────────────────────────────────────────────────────────────────────
    public interface IPurchaseInvoiceRepo
    {
        Task<string> GenerateInvoiceNumber();

        /// <summary>
        /// Generates the next unique nomor faktur pajak in FP-NNNNNNNNNN format.
        /// </summary>
        Task<string> GenerateTaxNumber();

        Task<int> InsertPurchaseInvoice(
            PurchaseInvoice model
        );

        Task<List<PurchaseInvoice>>
            GetAllPurchaseInvoice(int? id = null);

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

        /// <summary>
        /// Returns the oldest unpaid or partially-paid invoice for the given
        /// supplier, or null if no such invoice exists.
        /// An invoice qualifies when its status is NOT 'Paid' (i.e. 'Unpaid'
        /// or 'Partial').
        /// </summary>
        Task<PurchaseInvoice?> GetUnfinishedInvoiceBySupplier(
            int supplierId
        );

        /// <summary>
        /// Returns ALL unpaid / partially-paid invoices for the given supplier
        /// with their real-time outstanding_amount calculated fresh from the DB.
        /// Used to populate the invoice dropdown in the Purchase Return
        /// settlement dialog so the user sees an accurate outstanding balance.
        /// </summary>
        Task<List<PurchaseInvoice>> GetUnpaidInvoicesBySupplier(
            int supplierId
        );

        /// <summary>
        /// Applies a cash-refund credit to an invoice by inserting a
        /// purchase_payment record with payment_method = 'Cash Refund Credit'.
        /// Returns the new payment id.
        /// </summary>
        Task<int> ApplyCreditToInvoice(
            int purchaseInvoiceId,
            decimal creditAmount,
            string invoiceNumber
        );

        /// <summary>
        /// Recalculates the outstanding_amount for a single invoice and
        /// updates its status column to 'Paid' or 'Unpaid' accordingly.
        /// Invoices already set to 'Cancelled' are left unchanged.
        /// </summary>
        Task SyncInvoiceStatus(int purchaseInvoiceId);

        /// <summary>
        /// Recalculates outstanding_amount for every non-Cancelled invoice
        /// and bulk-updates their status. Used for the one-time backfill
        /// of existing records.
        /// </summary>
        Task SyncAllInvoiceStatuses();
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
            // Query only rows that already follow the canonical INV-NNNNNNNNNN
            // format so that leftover legacy numbers can never corrupt the counter.
            const string query = @"
                SELECT TOP 1 invoice_number
                FROM purchase_invoice
                WHERE invoice_number LIKE 'INV-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
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
                    result.ToString() ?? "INV-0000000000";

                // Strip the "INV-" prefix (always 4 chars) before parsing
                string numericPart = lastInv.Substring(4);

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"INV-{nextNumber:D10}";
        }

        // GENERATE TAX NUMBER (Nomor Faktur Pajak)
        // Follows the same TOP-1 + canonical-format pattern as GenerateInvoiceNumber.
        // Canonical format: FP-NNNNNNNNNN (FP prefix, 10-digit zero-padded sequence).
        public async Task<string> GenerateTaxNumber()
        {
            const string query = @"
                SELECT TOP 1 nomor_faktur_pajak
                FROM purchase_invoice
                WHERE nomor_faktur_pajak LIKE 'FP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                ORDER BY purchase_invoice_id DESC";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using SqlCommand command = new SqlCommand(query, connection);

            object? result = await command.ExecuteScalarAsync();

            int nextNumber = 1;

            if (result != null && result != DBNull.Value)
            {
                string lastFp = result.ToString() ?? "FP-0000000000";
                // Strip the "FP-" prefix (always 3 chars) before parsing
                string numericPart = lastFp.Substring(3);
                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"FP-{nextNumber:D10}";
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
                    nomor_faktur_pajak,
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
                    @nomor_faktur_pajak,
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

                    command.Parameters.AddWithValue(
                        "@nomor_faktur_pajak",
                        (object?)model.nomor_faktur_pajak ?? DBNull.Value
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

        // Reuses GetAllPurchaseInvoice's fully-JOINed query (supplier_name,
        // dp_paid, payment_paid, outstanding_amount, transaction_name, dst.)
        // filtered to one row -- matches exactly what the old detail modal
        // displayed, instead of the bare handful of columns this method used
        // to return on its own.
        public async Task<PurchaseInvoice?>
            GetPurchaseInvoiceById(
                int id
            )
        {
            var results = await GetAllPurchaseInvoice(id);
            return results.FirstOrDefault();
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

        // GET ALL UNPAID INVOICES BY SUPPLIER
        // Returns every invoice for the supplier that is not yet fully paid,
        // with the outstanding_amount computed fresh (total - dp - payments).
        // Used to populate the settlement-dialog dropdown so the user sees
        // the correct current balance for every candidate invoice.
        public async Task<List<PurchaseInvoice>> GetUnpaidInvoicesBySupplier(
            int supplierId
        )
        {
            const string query = @"
                SELECT
                    pi.purchase_invoice_id,
                    pi.goods_receipt_id,
                    pi.invoice_number,
                    pi.invoice_date,
                    pi.supplier_id,
                    pi.total_amount,
                    pi.status,
                    pi.created_at,
                    ms.supplier_name,

                    ISNULL(
                        (
                            SELECT SUM(pdp.amount)
                            FROM purchase_down_payment pdp
                            WHERE pdp.purchase_order_id = gr.purchase_order_id
                        ), 0
                    ) AS dp_paid,

                    pi.total_amount
                    - ISNULL(
                        (
                            SELECT SUM(pdp.amount)
                            FROM purchase_down_payment pdp
                            WHERE pdp.purchase_order_id = gr.purchase_order_id
                        ), 0)
                    - ISNULL(
                        (
                            SELECT SUM(pp.amount)
                            FROM purchase_payment pp
                            WHERE pp.purchase_invoice_id = pi.purchase_invoice_id
                        ), 0)
                    AS outstanding_amount

                FROM purchase_invoice pi
                LEFT JOIN goods_receipt gr
                    ON pi.goods_receipt_id = gr.goods_receipt_id
                LEFT JOIN master_supplier ms
                    ON pi.supplier_id = ms.supplier_id
                WHERE pi.supplier_id = @supplier_id
                  AND pi.status <> 'Paid'
                ORDER BY pi.purchase_invoice_id ASC";

            var results = new List<PurchaseInvoice>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@supplier_id", supplierId);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new PurchaseInvoice
                        {
                            purchase_invoice_id =
                                Convert.ToInt32(reader["purchase_invoice_id"]),
                            goods_receipt_id =
                                Convert.ToInt32(reader["goods_receipt_id"]),
                            invoice_number =
                                reader["invoice_number"]?.ToString() ?? "",
                            invoice_date =
                                Convert.ToDateTime(reader["invoice_date"]),
                            supplier_id =
                                Convert.ToInt32(reader["supplier_id"]),
                            total_amount =
                                Convert.ToDecimal(reader["total_amount"]),
                            status =
                                reader["status"]?.ToString() ?? "",
                            created_at =
                                Convert.ToDateTime(reader["created_at"]),
                            supplier_name =
                                reader["supplier_name"]?.ToString() ?? "",
                            dp_paid =
                                reader["dp_paid"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["dp_paid"]),
                            outstanding_amount =
                                reader["outstanding_amount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["outstanding_amount"])
                        });
                    }
                }
            }

            return results;
        }

        // GET UNFINISHED INVOICE BY SUPPLIER
        // Returns the oldest invoice that is not yet fully paid for the
        // given supplier.  Qualifies when status <> 'Paid' AND the computed
        // outstanding_amount (total - dp_paid - payments) > 0.
        public async Task<PurchaseInvoice?> GetUnfinishedInvoiceBySupplier(
            int supplierId
        )
        {
            const string query = @"
                SELECT TOP 1
                    pi.purchase_invoice_id,
                    pi.goods_receipt_id,
                    pi.invoice_number,
                    pi.invoice_date,
                    pi.supplier_id,
                    pi.total_amount,
                    pi.status,
                    pi.created_at,

                    ISNULL(
                        (
                            SELECT SUM(pdp.amount)
                            FROM purchase_down_payment pdp
                            WHERE pdp.purchase_order_id = gr.purchase_order_id
                        ), 0
                    ) AS dp_paid,

                    pi.total_amount
                    - ISNULL(
                        (
                            SELECT SUM(pdp.amount)
                            FROM purchase_down_payment pdp
                            WHERE pdp.purchase_order_id = gr.purchase_order_id
                        ), 0)
                    - ISNULL(
                        (
                            SELECT SUM(pp.amount)
                            FROM purchase_payment pp
                            WHERE pp.purchase_invoice_id = pi.purchase_invoice_id
                        ), 0)
                    AS outstanding_amount

                FROM purchase_invoice pi
                LEFT JOIN goods_receipt gr
                    ON pi.goods_receipt_id = gr.goods_receipt_id
                WHERE pi.supplier_id = @supplier_id
                  AND pi.status <> 'Paid'
                ORDER BY pi.purchase_invoice_id ASC";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@supplier_id", supplierId);

                using (SqlDataReader reader =
                    await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new PurchaseInvoice
                        {
                            purchase_invoice_id =
                                Convert.ToInt32(reader["purchase_invoice_id"]),
                            goods_receipt_id =
                                Convert.ToInt32(reader["goods_receipt_id"]),
                            invoice_number =
                                reader["invoice_number"]?.ToString() ?? "",
                            invoice_date =
                                Convert.ToDateTime(reader["invoice_date"]),
                            supplier_id =
                                Convert.ToInt32(reader["supplier_id"]),
                            total_amount =
                                Convert.ToDecimal(reader["total_amount"]),
                            status =
                                reader["status"]?.ToString() ?? "",
                            created_at =
                                Convert.ToDateTime(reader["created_at"]),
                            dp_paid =
                                reader["dp_paid"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["dp_paid"]),
                            outstanding_amount =
                                reader["outstanding_amount"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(reader["outstanding_amount"])
                        };
                    }
                }
            }

            return null;
        }

        // APPLY CREDIT TO INVOICE
        // Inserts a purchase_payment row with payment_method = 'Cash Refund Credit'
        // so that the outstanding_amount calculation for that invoice is reduced.
        // Returns the new purchase_payment_id.
        public async Task<int> ApplyCreditToInvoice(
            int purchaseInvoiceId,
            decimal creditAmount,
            string invoiceNumber
        )
        {
            // Generate a payment number in the same PAY-series
            string paymentNumber;
            const string numQuery = @"
                SELECT TOP 1 payment_number
                FROM purchase_payment
                WHERE payment_number LIKE 'PAY-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                ORDER BY purchase_payment_id DESC";

            using (SqlConnection cn = new SqlConnection(_connectionString))
            using (SqlCommand cm = new SqlCommand(numQuery, cn))
            {
                await cn.OpenAsync();
                object? r = await cm.ExecuteScalarAsync();
                int next = 1;
                if (r != null && r != DBNull.Value)
                {
                    string last = r.ToString() ?? "PAY-0000000000";
                    if (int.TryParse(last.Substring(4), out int parsed))
                        next = parsed + 1;
                }
                paymentNumber = $"PAY-{next:D10}";
            }

            const string insertQuery = @"
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
                    GETDATE(),
                    @amount,
                    'Cash Refund Credit',
                    'Confirmed',
                    @notes,
                    GETDATE()
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlConnection connection =
                new SqlConnection(_connectionString))
            using (SqlCommand command =
                new SqlCommand(insertQuery, connection))
            {
                await connection.OpenAsync();
                command.Parameters.AddWithValue("@payment_number",      paymentNumber);
                command.Parameters.AddWithValue("@purchase_invoice_id", purchaseInvoiceId);
                command.Parameters.AddWithValue("@amount",              creditAmount);
                command.Parameters.AddWithValue("@notes",
                    $"Cash refund credit from purchase return against invoice {invoiceNumber}");

                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }

        // SYNC INVOICE STATUS (single invoice)
        // Recomputes outstanding and flips status to Paid/Unpaid.
        // Cancelled invoices are never touched.
        public async Task SyncInvoiceStatus(int purchaseInvoiceId)
        {
            const string query = @"
                UPDATE pi
                SET pi.status = CASE
                    WHEN pi.status = 'Cancelled' THEN pi.status
                    WHEN (
                        pi.total_amount
                        - ISNULL((
                            SELECT SUM(pdp.amount)
                            FROM purchase_down_payment pdp
                            WHERE pdp.purchase_order_id = gr.purchase_order_id
                          ), 0)
                        - ISNULL((
                            SELECT SUM(pp.amount)
                            FROM purchase_payment pp
                            WHERE pp.purchase_invoice_id = pi.purchase_invoice_id
                          ), 0)
                    ) <= 0 THEN 'Paid'
                    ELSE 'Unpaid'
                END
                FROM purchase_invoice pi
                LEFT JOIN goods_receipt gr
                    ON pi.goods_receipt_id = gr.goods_receipt_id
                WHERE pi.purchase_invoice_id = @purchase_invoice_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            command.Parameters.AddWithValue("@purchase_invoice_id", purchaseInvoiceId);
            await command.ExecuteNonQueryAsync();
        }

        // SYNC ALL INVOICE STATUSES (backfill)
        // Touches every non-Cancelled invoice in one statement.
        public async Task SyncAllInvoiceStatuses()
        {
            const string query = @"
                UPDATE pi
                SET pi.status = CASE
                    WHEN (
                        pi.total_amount
                        - ISNULL((
                            SELECT SUM(pdp.amount)
                            FROM purchase_down_payment pdp
                            WHERE pdp.purchase_order_id = gr.purchase_order_id
                          ), 0)
                        - ISNULL((
                            SELECT SUM(pp.amount)
                            FROM purchase_payment pp
                            WHERE pp.purchase_invoice_id = pi.purchase_invoice_id
                          ), 0)
                    ) <= 0 THEN 'Paid'
                    ELSE 'Unpaid'
                END
                FROM purchase_invoice pi
                LEFT JOIN goods_receipt gr
                    ON pi.goods_receipt_id = gr.goods_receipt_id
                WHERE pi.status <> 'Cancelled'";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        // GET ALL (atau satu baris saja kalau `id` diisi -- dipakai juga oleh
        // GetPurchaseInvoiceById supaya field yang dikembalikan konsisten,
        // termasuk dp_paid/payment_paid/outstanding_amount yang sebelumnya
        // cuma tersedia lewat query list ini)
        public async Task<List<PurchaseInvoice>>
            GetAllPurchaseInvoice(int? id = null)
        {
        string query = @"
        SELECT
            pi.purchase_invoice_id,
            pi.goods_receipt_id,
            pi.invoice_number,
            pi.invoice_date,
            pi.supplier_id,
            pi.total_amount,
            pi.status,
            pi.created_at,
            ms.supplier_name,
            po.transaction_name,
            po.transaction_detail,
            po.tax_percentage,
            po.tax_amount,
            COALESCE(NULLIF(pi.nomor_faktur_pajak, ''), NULLIF(po.nomor_faktur_pajak, '')) AS nomor_faktur_pajak_resolved,

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
                (
                    SELECT SUM(pp.amount)
                    FROM purchase_payment pp
                    WHERE pp.purchase_invoice_id =
                        pi.purchase_invoice_id
                ),
                0
            ) AS payment_paid,

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

        LEFT JOIN purchase_order po
            ON gr.purchase_order_id =
            po.purchase_order_id

        LEFT JOIN master_supplier ms
            ON pi.supplier_id =
            ms.supplier_id
        " + (id.HasValue ? "WHERE pi.purchase_invoice_id = @id" : "") + @"
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
                    if (id.HasValue)
                        command.Parameters.AddWithValue("@id", id.Value);

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

                                     payment_paid =
                                        reader["payment_paid"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(
                                                reader["payment_paid"]
                                            ),

                                    outstanding_amount =
                                        reader["outstanding_amount"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(
                                                reader["outstanding_amount"]
                                            ),

                                    transaction_name =
                                        reader["transaction_name"] == DBNull.Value
                                            ? null
                                            : reader["transaction_name"]?.ToString(),

                                    transaction_detail =
                                        reader["transaction_detail"] == DBNull.Value
                                            ? null
                                            : reader["transaction_detail"]?.ToString(),

                                    nomor_faktur_pajak =
                                        reader.SafeGetString("nomor_faktur_pajak_resolved"),
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