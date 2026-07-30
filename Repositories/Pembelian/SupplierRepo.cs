using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface ISupplierRepo
    {
        Task MigrateSupplierCodes();

        Task<string> GenerateSupplierCode();

        Task<Supplier?> InsertSupplier(
            Supplier model
        );

        Task<List<Supplier>>
            GetAllSupplier();

        Task<Supplier?>
            GetSupplierById(int id);

        Task<bool>
            UpdateSupplier(Supplier model);

        Task<bool>
            DeleteSupplier(int id);
    }

    public class SupplierRepo
        : ISupplierRepo
    {
        private readonly string _connectionString;

        public SupplierRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString =
                options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // ─── MIGRATE EXISTING CODES ─────────────

        public async Task MigrateSupplierCodes()
        {
            // For every row whose supplier_code is NOT already in the correct
            // SUP-XXXXXXXXXX (10-digit) format, rewrite it using the numeric
            // part of the old code if one exists, otherwise fall back to the
            // row's own supplier_id.  All assignments are deterministic and
            // idempotent — running this twice is safe.
            const string query = @"
                UPDATE master_supplier
                SET supplier_code = 'SUP-' + RIGHT(
                    '0000000000' + CAST(
                        CASE
                            -- already well-formed: keep the existing number
                            WHEN supplier_code LIKE
                                'SUP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                            THEN CAST(SUBSTRING(supplier_code, 5, 10) AS BIGINT)

                            -- old short format e.g. SUP-001, Sup-12
                            WHEN supplier_code LIKE 'SUP-%'
                              OR supplier_code LIKE 'Sup-%'
                            THEN CAST(
                                SUBSTRING(
                                    supplier_code,
                                    PATINDEX('%[0-9]%', supplier_code),
                                    LEN(supplier_code)
                                ) AS BIGINT
                            )

                            -- no numeric part at all (e.g. ""Emmaaaaaa"")
                            ELSE supplier_id
                        END
                    AS NVARCHAR(20)),
                10)
                WHERE supplier_code NOT LIKE
                    'SUP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using SqlCommand command =
                new SqlCommand(query, connection);

            await command.ExecuteNonQueryAsync();
        }

        // ─── GENERATE SUPPLIER CODE ─────────────

        public async Task<string>
            GenerateSupplierCode()
        {
            // Atomically increment the counter table inside a SERIALIZABLE
            // transaction with UPDLOCK + HOLDLOCK. This guarantees that
            // concurrent sessions queue up behind the lock — no two
            // sessions can ever read the same counter value.
            //
            // Strategy:
            // 1. Start SERIALIZABLE transaction
            // 2. Read counter row WITH (UPDLOCK, HOLDLOCK)
            // 3. Increment counter
            // 4. Update counter table
            // 5. Commit
            // 6. Format as SUP-XXXXXXXXXX

            const string incrementQuery = @"
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                DECLARE @next BIGINT;

                -- Lock the counter row to prevent concurrent reads
                SELECT @next = last_value + 1
                FROM   dbo.supplier_code_counter WITH (UPDLOCK, HOLDLOCK)
                WHERE  id = 1;

                -- Persist the incremented value
                UPDATE dbo.supplier_code_counter
                SET    last_value = @next
                WHERE  id = 1;

                COMMIT TRANSACTION;

                -- Return the new code
                SELECT 'SUP-' + RIGHT('0000000000' + CAST(@next AS NVARCHAR(20)), 10);
            ";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var cmd = new SqlCommand(incrementQuery, connection);
            
            object? result = await cmd.ExecuteScalarAsync();
            
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException(
                    "Failed to generate supplier code: counter table may not be initialized. " +
                    "Run Migrations/add_supplier_code_sequence.sql first."
                );

            return result.ToString()!;
        }

        // ─── INSERT ─────────────────────────────

        // Ensures any code coming in is stored as SUP-XXXXXXXXXX.
        // Strips any existing SUP-/Sup- prefix, parses the numeric part,
        // falls back to 0 for fully non-numeric values (migrate endpoint
        // will clean those up properly using the real supplier_id).
        private static string FormatSupplierCode(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            // Already correct format — return as-is
            if (System.Text.RegularExpressions.Regex.IsMatch(
                    raw, @"^SUP-\d{10}$"))
                return raw;

            // Extract numeric portion after any prefix
            var digits = System.Text.RegularExpressions.Regex
                .Replace(raw, @"^[A-Za-z]+-?", "")
                .Trim();

            digits = System.Text.RegularExpressions.Regex
                .Replace(digits, @"\D", "");

            if (!long.TryParse(digits, out long num) || num == 0)
                return raw; // can't determine number — leave for migration

            return $"SUP-{num:D10}";
        }

        public async Task<Supplier?>
            InsertSupplier(
                Supplier model
            )
        {
            // If the caller provided a code, format it; otherwise generate
            // one atomically inside the same transaction as the INSERT.
            // This eliminates the race window — code generation + INSERT
            // happen in a single SERIALIZABLE transaction.
            bool needsGeneration = string.IsNullOrWhiteSpace(model.supplier_code);

            if (!needsGeneration)
            {
                model.supplier_code = FormatSupplierCode(model.supplier_code);
            }

            const string insertWithGeneratedCode = @"
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                -- Atomically increment the counter
                DECLARE @next BIGINT;
                SELECT @next = last_value + 1
                FROM   dbo.supplier_code_counter WITH (UPDLOCK, HOLDLOCK)
                WHERE  id = 1;

                UPDATE dbo.supplier_code_counter
                SET    last_value = @next
                WHERE  id = 1;

                DECLARE @generatedCode NVARCHAR(20);
                SET @generatedCode = 'SUP-' + RIGHT('0000000000' + CAST(@next AS NVARCHAR(20)), 10);

                -- Insert the supplier row with the generated code
                INSERT INTO master_supplier
                (
                    supplier_code,
                    supplier_name,
                    category_supplier,
                    no_telp_bisnis,
                    alamat,
                    email,
                    status
                )
                VALUES
                (
                    @generatedCode,
                    @supplier_name,
                    @category_supplier,
                    @no_telp_bisnis,
                    @alamat,
                    @email,
                    @status
                );

                DECLARE @newId INT = CAST(SCOPE_IDENTITY() AS INT);

                COMMIT TRANSACTION;

                -- Return both the new ID and the generated code
                SELECT @newId AS supplier_id, @generatedCode AS supplier_code;
            ";

            const string insertWithProvidedCode = @"
                INSERT INTO master_supplier
                (
                    supplier_code,
                    supplier_name,
                    category_supplier,
                    no_telp_bisnis,
                    alamat,
                    email,
                    status
                )
                VALUES
                (
                    @supplier_code,
                    @supplier_name,
                    @category_supplier,
                    @no_telp_bisnis,
                    @alamat,
                    @email,
                    @status
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                if (needsGeneration)
                {
                    // Path 1: Generate code atomically inside the INSERT transaction
                    using SqlCommand command = new SqlCommand(insertWithGeneratedCode, connection);

                    command.Parameters.AddWithValue("@supplier_name", model.supplier_name);
                    command.Parameters.AddWithValue("@category_supplier", (object?)model.category_supplier ?? DBNull.Value);
                    command.Parameters.AddWithValue("@no_telp_bisnis", model.no_telp_bisnis ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@alamat", model.alamat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@email", model.email ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@status", model.status ?? (object)DBNull.Value);

                    using SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        model.supplier_id = reader.GetInt32(0);
                        model.supplier_code = reader.GetString(1);
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to insert supplier: no result returned.");
                    }
                }
                else
                {
                    // Path 2: Caller provided a code — simple INSERT
                    using SqlCommand command = new SqlCommand(insertWithProvidedCode, connection);

                    command.Parameters.AddWithValue("@supplier_code", model.supplier_code);
                    command.Parameters.AddWithValue("@supplier_name", model.supplier_name);
                    command.Parameters.AddWithValue("@category_supplier", (object?)model.category_supplier ?? DBNull.Value);
                    command.Parameters.AddWithValue("@no_telp_bisnis", model.no_telp_bisnis ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@alamat", model.alamat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@email", model.email ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@status", model.status ?? (object)DBNull.Value);

                    int newId = Convert.ToInt32(await command.ExecuteScalarAsync());
                    model.supplier_id = newId;
                }

                return model;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InsertSupplier] {ex.Message}");
                throw;
            }
        }

        // ─── UPDATE ─────────────────────────────

        public async Task<bool>
            UpdateSupplier(
                Supplier model
            )
        {
            // Ensure the code stays in the correct format on every edit
            if (!string.IsNullOrWhiteSpace(model.supplier_code))
                model.supplier_code =
                    FormatSupplierCode(model.supplier_code);
            const string query = @"

                UPDATE master_supplier

                SET
                    supplier_code =
                        @supplier_code,

                    supplier_name =
                        @supplier_name,

                    category_supplier =
                        @category_supplier,

                    no_telp_bisnis =
                        @no_telp_bisnis,

                    alamat =
                        @alamat,

                    email =
                        @email,

                    status =
                        @status

                WHERE supplier_id =
                    @supplier_id";

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )

                using (
                    SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        )
                )
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@supplier_id",
                        model.supplier_id
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_code",
                        model.supplier_code
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_name",
                        model.supplier_name
                    );

                    command.Parameters.AddWithValue(
                        "@category_supplier",
                        (object?)model.category_supplier
                        ?? DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@no_telp_bisnis",
                        model.no_telp_bisnis
                        ?? (object)DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@alamat",
                        model.alamat
                        ?? (object)DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@email",
                        model.email
                        ?? (object)DBNull.Value
                    );

                    command.Parameters.AddWithValue(
                        "@status",
                        model.status
                        ?? (object)DBNull.Value
                    );

                    int result =
                        await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }

            catch (Exception)
            {
                return false;
            }
        }

        // ─── DELETE ─────────────────────────────

        public async Task<bool>
            DeleteSupplier(
                int id
            )
        {
            // Delete all dependent rows first to satisfy foreign key constraints,
            // then remove the supplier itself.
            const string query = @"
                -- 1. payments that reference invoices for this supplier
                DELETE pp
                FROM purchase_payment pp
                INNER JOIN purchase_invoice pi
                    ON pp.purchase_invoice_id = pi.purchase_invoice_id
                WHERE pi.supplier_id = @id;

                -- 2. invoices for this supplier
                DELETE FROM purchase_invoice
                WHERE supplier_id = @id;

                -- 3. down payments for this supplier
                DELETE FROM purchase_down_payment
                WHERE supplier_id = @id;

                -- 4. purchase order details for POs belonging to this supplier
                DELETE pod
                FROM purchase_order_detail pod
                INNER JOIN purchase_order po
                    ON pod.purchase_order_id = po.purchase_order_id
                WHERE po.supplier_id = @id;

                -- 5. goods receipt details for GRs linked to POs of this supplier
                DELETE grd
                FROM goods_receipt_detail grd
                INNER JOIN goods_receipt gr
                    ON grd.goods_receipt_id = gr.goods_receipt_id
                INNER JOIN purchase_order po
                    ON gr.purchase_order_id = po.purchase_order_id
                WHERE po.supplier_id = @id;

                -- 6. goods receipts for POs of this supplier
                DELETE gr
                FROM goods_receipt gr
                INNER JOIN purchase_order po
                    ON gr.purchase_order_id = po.purchase_order_id
                WHERE po.supplier_id = @id;

                -- 7. purchase orders for this supplier
                DELETE FROM purchase_order
                WHERE supplier_id = @id;

                -- 8. supplier catalog / products
                DELETE FROM supplier_products
                WHERE supplier_id = @id;

                -- 9. finally, the supplier itself
                DELETE FROM master_supplier
                WHERE supplier_id = @id;
            ";

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )

                using (
                    SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        )
                )
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

            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[DeleteSupplier] {ex.Message}"
                );
                return false;
            }
        }

        // ─── GET ALL ────────────────────────────

        public async Task<List<Supplier>>
            GetAllSupplier()
        {
            const string query =
                @"SELECT * FROM master_supplier";

            var response =
                new List<Supplier>();

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )

                using (
                    SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        )
                )
                {
                    await connection.OpenAsync();

                    using (
                        SqlDataReader reader =
                            await command.ExecuteReaderAsync()
                    )
                    {
                        // Performance: cache ordinal positions once before the
                        // read loop — avoids a string scan per row per column.
                        // Mapping behaviour and returned model are unchanged.
                        int ord_supplier_id       = reader.GetOrdinal("supplier_id");
                        int ord_supplier_code     = reader.GetOrdinal("supplier_code");
                        int ord_supplier_name     = reader.GetOrdinal("supplier_name");
                        int ord_category_supplier = reader.GetOrdinal("category_supplier");
                        int ord_no_telp_bisnis    = reader.GetOrdinal("no_telp_bisnis");
                        int ord_alamat            = reader.GetOrdinal("alamat");
                        int ord_email             = reader.GetOrdinal("email");
                        int ord_status            = reader.GetOrdinal("status");

                        while (
                            await reader.ReadAsync()
                        )
                        {
                            var supplier =
                                new Supplier()
                                {
                                    supplier_id =
                                        reader.GetInt32(ord_supplier_id),

                                    supplier_code =
                                        reader[ord_supplier_code]
                                            .ToString(),

                                    supplier_name =
                                        reader[ord_supplier_name]
                                            .ToString(),

                                    category_supplier =
                                        reader.GetInt32(ord_category_supplier),

                                    no_telp_bisnis =
                                        reader[ord_no_telp_bisnis]
                                            .ToString(),

                                    alamat =
                                        reader[ord_alamat]
                                            .ToString(),

                                    email =
                                        reader[ord_email]
                                            .ToString(),

                                    status =
                                        reader[ord_status]
                                            .ToString()
                                };

                            response.Add(
                                supplier
                            );
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                var msg =
                    ex.Message;

                throw new Exception(msg);
            }

            return response;
        }

        // ─── GET BY ID ──────────────────────────

        public async Task<Supplier?>
            GetSupplierById(
                int id
            )
        {
            const string query = @"

                SELECT *
                FROM master_supplier

                WHERE supplier_id =
                    @id";

            try
            {
                using (
                    SqlConnection connection =
                        new SqlConnection(
                            _connectionString
                        )
                )

                using (
                    SqlCommand command =
                        new SqlCommand(
                            query,
                            connection
                        )
                )
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@id",
                        id
                    );

                    using (
                        SqlDataReader reader =
                            await command.ExecuteReaderAsync()
                    )
                    {
                        if (
                            await reader.ReadAsync()
                        )
                        {
                            // Performance: cache ordinals before accessing columns
                            // even for a single-row result — avoids repeated string
                            // scans across multiple column accesses. Logic unchanged.
                            int ord_supplier_id       = reader.GetOrdinal("supplier_id");
                            int ord_supplier_code     = reader.GetOrdinal("supplier_code");
                            int ord_supplier_name     = reader.GetOrdinal("supplier_name");
                            int ord_category_supplier = reader.GetOrdinal("category_supplier");
                            int ord_no_telp_bisnis    = reader.GetOrdinal("no_telp_bisnis");
                            int ord_alamat            = reader.GetOrdinal("alamat");
                            int ord_email             = reader.GetOrdinal("email");
                            int ord_status            = reader.GetOrdinal("status");

                            return new Supplier()
                            {
                                supplier_id =
                                    reader.GetInt32(ord_supplier_id),

                                supplier_code =
                                    reader[ord_supplier_code]
                                        .ToString(),

                                supplier_name =
                                    reader[ord_supplier_name]
                                        .ToString(),

                                category_supplier =
                                    reader.GetInt32(ord_category_supplier),

                                no_telp_bisnis =
                                    reader[ord_no_telp_bisnis]
                                        .ToString(),

                                alamat =
                                    reader[ord_alamat]
                                        .ToString(),

                                email =
                                    reader[ord_email]
                                        .ToString(),

                                status =
                                    reader[ord_status]
                                        .ToString()
                            };
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                var msg =
                    ex.Message;

                throw new Exception(msg);
            }

            return null;
        }
    }
}