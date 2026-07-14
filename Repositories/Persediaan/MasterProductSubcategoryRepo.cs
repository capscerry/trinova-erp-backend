using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterProductSubcategoryRepo
    {
        private readonly string _connectionString;

        public MasterProductSubcategoryRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        public async Task<List<ProductSubcategory>> GetAllAsync()
        {
            const string query = @"
                SELECT
                    s.subcategory_id,
                    s.category_id,
                    s.subcategory_code,
                    s.subcategory_name,
                    s.is_active,
                    s.created_at,
                    s.updated_at,
                    c.category_name
                FROM master_product_subcategory s
                LEFT JOIN master_product_category c
                    ON s.category_id = c.category_id
                WHERE s.is_active = 1
                ORDER BY s.subcategory_name";

            var response = new List<ProductSubcategory>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        response.Add(new ProductSubcategory
                        {
                            subcategory_id = Convert.ToInt32(reader["subcategory_id"]),
                            category_id = Convert.ToInt32(reader["category_id"]),
                            code = reader["subcategory_code"]?.ToString() ?? "",
                            name = reader["subcategory_name"]?.ToString() ?? "",
                            is_active = Convert.ToBoolean(reader["is_active"]),
                            created_at = Convert.ToDateTime(reader["created_at"]),
                            updated_at = Convert.ToDateTime(reader["updated_at"]),

                            Category = new MasterProductCategory
                            {
                                category_id = Convert.ToInt32(reader["category_id"]),
                                category_name = reader["category_name"]?.ToString()
                            }
                        });
                    }
                }
            }

            return response;
        }

        public async Task<List<ProductSubcategory>> GetByCategoryAsync(
            int categoryId
        )
        {
            const string query = @"
                SELECT
                    s.subcategory_id,
                    s.category_id,
                    s.subcategory_code,
                    s.subcategory_name,
                    s.is_active,
                    s.created_at,
                    s.updated_at,
                    c.category_name
                FROM master_product_subcategory s
                LEFT JOIN master_product_category c
                    ON s.category_id = c.category_id
                WHERE s.category_id = @category_id
                  AND s.is_active = 1
                ORDER BY s.subcategory_name";

            var response = new List<ProductSubcategory>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@category_id", categoryId);

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        response.Add(new ProductSubcategory
                        {
                            subcategory_id = Convert.ToInt32(reader["subcategory_id"]),
                            category_id = Convert.ToInt32(reader["category_id"]),
                            code = reader["subcategory_code"]?.ToString() ?? "",
                            name = reader["subcategory_name"]?.ToString() ?? "",
                            is_active = Convert.ToBoolean(reader["is_active"]),
                            created_at = Convert.ToDateTime(reader["created_at"]),
                            updated_at = Convert.ToDateTime(reader["updated_at"]),

                            Category = new MasterProductCategory
                            {
                                category_id = Convert.ToInt32(reader["category_id"]),
                                category_name = reader["category_name"]?.ToString()
                            }
                        });
                    }
                }
            }

            return response;
        }

        public async Task<ProductSubcategory?> GetByIdAsync(int id)
        {
            const string query = @"
                SELECT TOP 1
                    s.subcategory_id,
                    s.category_id,
                    s.subcategory_code,
                    s.subcategory_name,
                    s.is_active,
                    s.created_at,
                    s.updated_at,
                    c.category_name
                FROM master_product_subcategory s
                LEFT JOIN master_product_category c
                    ON s.category_id = c.category_id
                WHERE s.subcategory_id = @subcategory_id";

            ProductSubcategory? response = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@subcategory_id", id);

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        response = new ProductSubcategory
                        {
                            subcategory_id = Convert.ToInt32(reader["subcategory_id"]),
                            category_id = Convert.ToInt32(reader["category_id"]),
                            code = reader["subcategory_code"]?.ToString() ?? "",
                            name = reader["subcategory_name"]?.ToString() ?? "",
                            is_active = Convert.ToBoolean(reader["is_active"]),
                            created_at = Convert.ToDateTime(reader["created_at"]),
                            updated_at = Convert.ToDateTime(reader["updated_at"]),

                            Category = new MasterProductCategory
                            {
                                category_id = Convert.ToInt32(reader["category_id"]),
                                category_name = reader["category_name"]?.ToString()
                            }
                        };
                    }
                }
            }

            return response;
        }

        public async Task<ProductSubcategory?> GetByNameAsync(string name)
        {
            const string query = @"
                SELECT TOP 1 *
                FROM master_product_subcategory
                WHERE subcategory_name = @subcategory_name
                  AND is_active = 1";

            ProductSubcategory? response = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@subcategory_name", name);

                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        response = new ProductSubcategory
                        {
                            subcategory_id = Convert.ToInt32(reader["subcategory_id"]),
                            category_id = Convert.ToInt32(reader["category_id"]),
                            code = reader["subcategory_code"]?.ToString() ?? "",
                            name = reader["subcategory_name"]?.ToString() ?? "",
                            is_active = Convert.ToBoolean(reader["is_active"]),
                            created_at = Convert.ToDateTime(reader["created_at"]),
                            updated_at = Convert.ToDateTime(reader["updated_at"])
                        };
                    }
                }
            }

            return response;
        }

        public async Task<ProductSubcategory> CreateAsync(
            ProductSubcategory subcategory
        )
        {
            const string query = @"
                INSERT INTO master_product_subcategory
                (
                    category_id,
                    subcategory_code,
                    subcategory_name,
                    is_active,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @category_id,
                    @subcategory_code,
                    @subcategory_name,
                    @is_active,
                    @created_at,
                    @updated_at
                )";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@category_id", subcategory.category_id);
                command.Parameters.AddWithValue("@subcategory_code", subcategory.code);
                command.Parameters.AddWithValue("@subcategory_name", subcategory.name);
                command.Parameters.AddWithValue("@is_active", true);
                command.Parameters.AddWithValue("@created_at", subcategory.created_at);
                command.Parameters.AddWithValue("@updated_at", subcategory.updated_at);

                await connection.OpenAsync();

                await command.ExecuteNonQueryAsync();
            }

            return subcategory;
        }

        public async Task<string> GenerateNextCodeAsync()
        {
            const string query = @"
                SELECT ISNULL(
                    MAX(
                        TRY_CAST(subcategory_code AS INT)
                    ),
                    0
                )
                FROM master_product_subcategory";

            using SqlConnection connection =
                new SqlConnection(_connectionString);

            using SqlCommand command =
                new SqlCommand(query, connection);

            await connection.OpenAsync();

            var result =
                await command.ExecuteScalarAsync();

            int nextCode =
                Convert.ToInt32(result) + 1;

            return nextCode.ToString("D2");
        }

        public async Task<ProductSubcategory?> UpdateAsync(
            ProductSubcategory subcategory
        )
        {
            const string query = @"
                UPDATE master_product_subcategory
                SET
                    category_id = @category_id,
                    subcategory_code = @subcategory_code,
                    subcategory_name = @subcategory_name,
                    updated_at = @updated_at
                WHERE subcategory_id = @subcategory_id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@subcategory_id", subcategory.subcategory_id);
                command.Parameters.AddWithValue("@category_id", subcategory.category_id);
                command.Parameters.AddWithValue("@subcategory_code", subcategory.code);
                command.Parameters.AddWithValue("@subcategory_name", subcategory.name);
                command.Parameters.AddWithValue("@updated_at", subcategory.updated_at);

                await connection.OpenAsync();

                await command.ExecuteNonQueryAsync();
            }

            return subcategory;
        }

        public async Task<bool> DeleteAsync(
            ProductSubcategory subcategory
        )
        {
            const string query = @"
                UPDATE master_product_subcategory
                SET
                    is_active = 0,
                    updated_at = @updated_at
                WHERE subcategory_id = @subcategory_id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue(
                    "@subcategory_id",
                    subcategory.subcategory_id
                );

                command.Parameters.AddWithValue(
                    "@updated_at",
                    DateTime.Now
                );

                await connection.OpenAsync();

                int result = await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }
    }
}