using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterProductCategoryRepo
    {
        private readonly string _connectionString;

        public MasterProductCategoryRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        // GET ALL
        public async Task<List<MasterProductCategory>> GetAllMasterProductCategory()
        {
            var response = new List<MasterProductCategory>();

            const string query = @"
                SELECT *
                FROM master_product_category
                WHERE is_active = 1";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(new MasterProductCategory
                {
                    category_id = Convert.ToInt32(reader["category_id"]),
                    category_name = reader["category_name"].ToString(),
                    created_at = Convert.ToDateTime(reader["created_at"]),
                    created_by = reader["created_by"].ToString(),
                    updated_at = Convert.ToDateTime(reader["updated_at"]),
                    updated_by = reader["updated_by"].ToString(),
                    is_active = Convert.ToBoolean(reader["is_active"])
                });
            }

            return response;
        }

        // GET BY ID
        public async Task<MasterProductCategory?> GetMasterProductCategoryById(int categoryId)
        {
            MasterProductCategory? response = null;

            const string query = @"
                SELECT *
                FROM master_product_category
                WHERE category_id = @category_id
                AND is_active = 1";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@category_id", categoryId);

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                response = new MasterProductCategory
                {
                    category_id = Convert.ToInt32(reader["category_id"]),
                    category_name = reader["category_name"].ToString(),
                    created_at = Convert.ToDateTime(reader["created_at"]),
                    created_by = reader["created_by"].ToString(),
                    updated_at = Convert.ToDateTime(reader["updated_at"]),
                    updated_by = reader["updated_by"].ToString(),
                    is_active = Convert.ToBoolean(reader["is_active"])
                };
            }

            return response;
        }

        // INSERT
        public async Task<bool> InsertMasterProductCategory(MasterProductCategory model)
        {
            const string query = @"
                INSERT INTO master_product_category
                (
                    category_name,
                    created_at,
                    created_by,
                    updated_at,
                    updated_by,
                    is_active
                )
                VALUES
                (
                    @category_name,
                    @created_at,
                    @created_by,
                    @updated_at,
                    @updated_by,
                    @is_active
                )";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@category_name", model.category_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@created_at", model.created_at ?? DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@created_by", model.created_by ?? "system");
            command.Parameters.AddWithValue("@updated_at", model.updated_at ?? DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@updated_by", model.updated_by ?? "system");
            command.Parameters.AddWithValue("@is_active", model.is_active ?? true);

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        // UPDATE
        public async Task<bool> UpdateMasterProductCategory(MasterProductCategory model)
        {
            const string query = @"
                UPDATE master_product_category
                SET
                    category_name = @category_name,
                    updated_at = @updated_at,
                    updated_by = @updated_by
                WHERE category_id = @category_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@category_id", model.category_id);
            command.Parameters.AddWithValue("@category_name", model.category_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@updated_by", model.updated_by ?? "system");

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        // SOFT DELETE
        public async Task<bool> DeleteMasterProductCategory(int categoryId)
        {
            const string query = @"
                UPDATE master_product_category
                SET
                    is_active = 0,
                    updated_at = @updated_at
                WHERE category_id = @category_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@category_id", categoryId);
            command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.AddHours(7));

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }
    }
}