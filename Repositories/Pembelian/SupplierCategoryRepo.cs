using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface ISupplierCategoryRepo
    {
        Task MigrateCategoryCodes();

        Task<string> GenerateCategoryCode();

        Task<bool> InsertSupplierCategory(SupplierCategory model);

        Task<List<SupplierCategory>> GetAllSupplierCategory();

        Task<bool> UpdateSupplierCategory(SupplierCategory model);

        Task<bool> IsCategoryUsed(int categoryId);

        Task<bool> DeleteSupplierCategory(int id);
    }

    public class SupplierCategoryRepo : ISupplierCategoryRepo
    {
        private readonly string _connectionString;

        public SupplierCategoryRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        // ─── SELF-HEALING SCHEMA + BACK-FILL ────────────────────────────────────
        // Adds category_code (if missing), adds is_active (if missing), then
        // back-fills any rows that still lack a valid SUC- code.
        // Idempotent — safe to call on every request.

        private async Task EnsureCategoryCodeColumn(SqlConnection connection)
        {
            // 1. Add category_code if absent
            const string addCode = @"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'supplier_category'
                      AND COLUMN_NAME = 'category_code'
                )
                BEGIN
                    ALTER TABLE supplier_category
                    ADD category_code NVARCHAR(20) NULL;
                END";

            // 2. Add is_active if absent (defaults existing rows to 1 = active)
            const string addActive = @"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME  = 'supplier_category'
                      AND COLUMN_NAME = 'is_active'
                )
                BEGIN
                    ALTER TABLE supplier_category
                    ADD is_active BIT NOT NULL DEFAULT 1;
                END";

            // 3. Back-fill missing / malformed category_code values
            const string backFill = @"
                UPDATE sc
                SET sc.category_code = 'SUC-' + RIGHT(
                    '0000000000' + CAST(rn.rn AS NVARCHAR(10)), 10
                )
                FROM supplier_category sc
                INNER JOIN (
                    SELECT
                        category_id,
                        ROW_NUMBER() OVER (ORDER BY category_id ASC) AS rn
                    FROM supplier_category
                    WHERE category_code IS NULL
                       OR category_code NOT LIKE
                            'SUC-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                ) rn ON sc.category_id = rn.category_id";

            using var cmd1 = new SqlCommand(addCode,   connection);
            await cmd1.ExecuteNonQueryAsync();

            using var cmd2 = new SqlCommand(addActive, connection);
            await cmd2.ExecuteNonQueryAsync();

            using var cmd3 = new SqlCommand(backFill,  connection);
            await cmd3.ExecuteNonQueryAsync();
        }

        // ─── MIGRATE (public endpoint) ───────────────────────────────────────────

        public async Task MigrateCategoryCodes()
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await EnsureCategoryCodeColumn(connection);
        }

        // ─── GENERATE NEXT CODE ──────────────────────────────────────────────────

        public async Task<string> GenerateCategoryCode()
        {
            const string maxQuery = @"
                SELECT ISNULL(
                    (
                        SELECT MAX(CAST(SUBSTRING(category_code, 5, 10) AS BIGINT))
                        FROM supplier_category
                        WHERE category_code LIKE
                            'SUC-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
                    ),
                    (SELECT ISNULL(MAX(category_id), 0) FROM supplier_category)
                )";

            const string existsQuery = @"
                SELECT COUNT(1) FROM supplier_category
                WHERE category_code = @code";

            using SqlConnection connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await EnsureCategoryCodeColumn(connection);

            long nextNumber;
            using (var cmd = new SqlCommand(maxQuery, connection))
            {
                object? result = await cmd.ExecuteScalarAsync();
                nextNumber = (result != null && result != DBNull.Value)
                    ? Convert.ToInt64(result) + 1
                    : 1;
            }

            using (var cmd = new SqlCommand(existsQuery, connection))
            {
                cmd.Parameters.Add("@code", System.Data.SqlDbType.NVarChar, 20);
                while (true)
                {
                    string candidate = $"SUC-{nextNumber:D10}";
                    cmd.Parameters["@code"].Value = candidate;
                    int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (count == 0) return candidate;
                    nextNumber++;
                }
            }
        }

        // ─── INSERT ──────────────────────────────────────────────────────────────

        public async Task<bool> InsertSupplierCategory(SupplierCategory model)
        {
            model.category_code = await GenerateCategoryCode();

            const string query = @"
                INSERT INTO supplier_category
                (
                    category_code,
                    category_name,
                    is_active,
                    created_by,
                    created_date
                )
                VALUES
                (
                    @category_code,
                    @category_name,
                    @is_active,
                    @created_by,
                    GETDATE()
                )";

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                using SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();

                command.Parameters.AddWithValue("@category_code", model.category_code);
                command.Parameters.AddWithValue("@category_name",  model.category_name);
                command.Parameters.AddWithValue("@is_active",      model.is_active);
                command.Parameters.AddWithValue("@created_by",     (object?)model.created_by ?? DBNull.Value);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        // ─── UPDATE ──────────────────────────────────────────────────────────────

        public async Task<bool> UpdateSupplierCategory(SupplierCategory model)
        {
            const string query = @"
                UPDATE supplier_category
                SET
                    category_name = @category_name,
                    is_active     = @is_active,
                    update_by     = @update_by,
                    update_date   = GETDATE()
                WHERE category_id = @category_id";

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                using SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();

                command.Parameters.AddWithValue("@category_id",   model.category_id);
                command.Parameters.AddWithValue("@category_name",  model.category_name);
                command.Parameters.AddWithValue("@is_active",      model.is_active);
                command.Parameters.AddWithValue("@update_by",      (object?)model.update_by ?? DBNull.Value);

                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch { return false; }
        }

        // ─── IS CATEGORY USED ────────────────────────────────────────────────────

        public async Task<bool> IsCategoryUsed(int categoryId)
        {
            const string query = @"
                SELECT COUNT(*) FROM master_supplier
                WHERE supplier_category_id = @categoryId";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            command.Parameters.AddWithValue("@categoryId", categoryId);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        // ─── DELETE ──────────────────────────────────────────────────────────────

        public async Task<bool> DeleteSupplierCategory(int id)
        {
            const string query = @"
                DELETE FROM supplier_category WHERE category_id = @id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);
            await connection.OpenAsync();
            command.Parameters.AddWithValue("@id", id);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        // ─── GET ALL ─────────────────────────────────────────────────────────────

        public async Task<List<SupplierCategory>> GetAllSupplierCategory()
        {
            var response = new List<SupplierCategory>();

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Ensure schema is up-to-date before querying
                await EnsureCategoryCodeColumn(connection);

                const string query = @"
                    SELECT
                        category_id,
                        category_code,
                        category_name,
                        is_active,
                        created_date,
                        created_by,
                        update_date,
                        update_by
                    FROM supplier_category
                    ORDER BY category_id ASC";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                // Performance: cache ordinal positions before the read loop so
                // GetOrdinal() (which does a string scan) is called once per
                // query rather than once per row. Mapping behaviour unchanged.
                int ord_category_id   = reader.GetOrdinal("category_id");
                int ord_category_code = reader.GetOrdinal("category_code");
                int ord_category_name = reader.GetOrdinal("category_name");
                int ord_is_active     = reader.GetOrdinal("is_active");
                int ord_created_date  = reader.GetOrdinal("created_date");
                int ord_created_by    = reader.GetOrdinal("created_by");
                int ord_update_date   = reader.GetOrdinal("update_date");
                int ord_update_by     = reader.GetOrdinal("update_by");

                while (await reader.ReadAsync())
                {
                    response.Add(new SupplierCategory
                    {
                        category_id   = reader.GetInt32(ord_category_id),
                        category_code = reader[ord_category_code]?.ToString() ?? string.Empty,
                        category_name = reader.GetString(ord_category_name),
                        is_active     = reader[ord_is_active] != DBNull.Value && Convert.ToBoolean(reader[ord_is_active]),
                        created_date  = reader[ord_created_date] as DateTime?,
                        created_by    = reader[ord_created_by]?.ToString(),
                        update_date   = reader[ord_update_date] as DateTime?,
                        update_by     = reader[ord_update_by]?.ToString()
                    });
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
