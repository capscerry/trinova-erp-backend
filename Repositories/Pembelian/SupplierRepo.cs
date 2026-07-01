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
            // Find the highest numeric suffix across all well-formed SUP- codes
            // AND fall back to MAX(supplier_id) for any rows whose code has no
            // extractable number (e.g. "Emmaaaaaa"), so every existing row is
            // accounted for in the sequence.
            const string query = @"
                SELECT ISNULL(
                    (
                        SELECT MAX(
                            CAST(SUBSTRING(supplier_code, 5, 10) AS BIGINT)
                        )
                        FROM master_supplier
                        WHERE supplier_code LIKE
                            'SUP-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                    ),
                    (
                        SELECT MAX(supplier_id)
                        FROM master_supplier
                    )
                )";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using SqlCommand command =
                new SqlCommand(query, connection);

            object? result =
                await command.ExecuteScalarAsync();

            long nextNumber = 1;

            if (result != null && result != DBNull.Value)
                nextNumber = Convert.ToInt64(result) + 1;

            return $"SUP-{nextNumber:D10}";
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
            // Always ensure a properly formatted code is stored
            model.supplier_code = string.IsNullOrWhiteSpace(model.supplier_code)
                ? await GenerateSupplierCode()
                : FormatSupplierCode(model.supplier_code);

            const string query = @"

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

                SELECT CAST(
                    SCOPE_IDENTITY()
                    AS INT
                );
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
                        "@supplier_code",
                        model.supplier_code
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_name",
                        model.supplier_name
                    );

                    command.Parameters.AddWithValue(
                        "@category_supplier",
                        model.category_supplier
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

                    int newId =
                        Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );

                    model.supplier_id =
                        newId;

                    return model;
                }
            }

            catch (Exception)
            {
                return null;
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
                        model.category_supplier
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
                        while (
                            await reader.ReadAsync()
                        )
                        {
                            var supplier =
                                new Supplier()
                                {
                                    supplier_id =
                                        reader.GetInt32(
                                            reader.GetOrdinal(
                                                "supplier_id"
                                            )
                                        ),

                                    supplier_code =
                                        reader.GetString(
                                            reader.GetOrdinal(
                                                "supplier_code"
                                            )
                                        ),

                                    supplier_name =
                                        reader.GetString(
                                            reader.GetOrdinal(
                                                "supplier_name"
                                            )
                                        ),

                                    category_supplier =
                                        reader.GetInt32(
                                            reader.GetOrdinal(
                                                "category_supplier"
                                            )
                                        ),

                                    no_telp_bisnis =
                                        reader["no_telp_bisnis"]
                                            .ToString(),

                                    alamat =
                                        reader["alamat"]
                                            .ToString(),

                                    email =
                                        reader["email"]
                                            .ToString(),

                                    status =
                                        reader["status"]
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
                            return new Supplier()
                            {
                                supplier_id =
                                    reader.GetInt32(
                                        reader.GetOrdinal(
                                            "supplier_id"
                                        )
                                    ),

                                supplier_code =
                                    reader["supplier_code"]
                                        .ToString(),

                                supplier_name =
                                    reader["supplier_name"]
                                        .ToString(),

                                category_supplier =
                                    reader.GetInt32(
                                        reader.GetOrdinal(
                                            "category_supplier"
                                        )
                                    ),

                                no_telp_bisnis =
                                    reader["no_telp_bisnis"]
                                        .ToString(),

                                alamat =
                                    reader["alamat"]
                                        .ToString(),

                                email =
                                    reader["email"]
                                        .ToString(),

                                status =
                                    reader["status"]
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