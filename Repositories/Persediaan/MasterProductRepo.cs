using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterProductRepo
    {
        private readonly string _connectionString;

        public MasterProductRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // INSERT
        public async Task<bool> InsertMasterProduct(
            MasterProduct model
        )
        {
            const string query = @"
                INSERT INTO master_product
                (
                    product_name,
                    product_code,
                    product_type,
                    uom_id,
                    category_id,
                    subcategory_id,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @product_name,
                    @product_code,
                    @product_type,
                    @uom_id,
                    @category_id,
                    @subcategory_id,
                    @created_at,
                    @updated_at
                )";

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                using SqlCommand command =
                    new SqlCommand(query, connection);

                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@product_name",
                    model.product_name ?? (object)DBNull.Value
                );

                command.Parameters.AddWithValue(
                    "@product_code",
                    model.product_code ?? (object)DBNull.Value
                );

                command.Parameters.AddWithValue(
                    "@product_type",
                    model.product_type ?? (object)DBNull.Value
                );

                command.Parameters.AddWithValue(
                    "@uom_id",
                    model.uom_id
                );

                command.Parameters.AddWithValue(
                    "@category_id",
                    model.category_id
                );

                command.Parameters.AddWithValue(
                    "@subcategory_id",
                    model.subcategory_id
                );

                command.Parameters.AddWithValue(
                    "@created_at",
                    model.created_at ?? DateTime.Now
                );

                command.Parameters.AddWithValue(
                    "@updated_at",
                    model.updated_at ?? DateTime.Now
                );

                int result =
                    await command.ExecuteNonQueryAsync();

                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        // GET ALL
        public async Task<List<MasterProduct>>
            GetAllMasterProduct()
        {
            const string query = @"
                SELECT *
                FROM master_product
                ORDER BY product_id DESC";

            var response =
                new List<MasterProduct>();

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                using SqlCommand command =
                    new SqlCommand(query, connection);

                await connection.OpenAsync();

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    response.Add(
                        new MasterProduct
                        {
                            product_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "product_id"
                                    )
                                ),

                            product_name =
                                reader["product_name"]
                                    ?.ToString(),

                            product_code =
                                reader["product_code"]
                                    ?.ToString(),

                            product_type =
                                reader["product_type"]
                                    ?.ToString(),

                            uom_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "uom_id"
                                    )
                                ),

                            category_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "category_id"
                                    )
                                ),

                            subcategory_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "subcategory_id"
                                    )
                                ),

                            created_at =
                                reader["created_at"]
                                    == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(
                                        reader["created_at"]
                                    ),

                            updated_at =
                                reader["updated_at"]
                                    == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(
                                        reader["updated_at"]
                                    )
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return response;
        }

        // GET BY ID
        public async Task<MasterProduct?>
            GetMasterProductById(
                int productId
            )
        {
            const string query = @"
                SELECT *
                FROM master_product
                WHERE product_id = @product_id";

            MasterProduct? response = null;

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@product_id",
                    productId
                );

                await connection.OpenAsync();

                using SqlDataReader reader =
                    await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    response =
                        new MasterProduct
                        {
                            product_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "product_id"
                                    )
                                ),

                            product_name =
                                reader["product_name"]
                                    ?.ToString(),

                            product_code =
                                reader["product_code"]
                                    ?.ToString(),

                            product_type =
                                reader["product_type"]
                                    ?.ToString(),

                            uom_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "uom_id"
                                    )
                                ),

                            category_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "category_id"
                                    )
                                ),

                            subcategory_id =
                                reader.GetInt32(
                                    reader.GetOrdinal(
                                        "subcategory_id"
                                    )
                                ),

                            created_at =
                                reader["created_at"]
                                    == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(
                                        reader["created_at"]
                                    ),

                            updated_at =
                                reader["updated_at"]
                                    == DBNull.Value
                                    ? null
                                    : Convert.ToDateTime(
                                        reader["updated_at"]
                                    )
                        };
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return response;
        }

        // UPDATE
        public async Task<bool> UpdateMasterProduct(
            MasterProduct model
        )
        {
            const string query = @"
                UPDATE master_product
                SET
                    product_name = @product_name,
                    product_code = @product_code,
                    product_type = @product_type,
                    uom_id = @uom_id,
                    category_id = @category_id,
                    subcategory_id = @subcategory_id,
                    updated_at = @updated_at
                WHERE product_id = @product_id";

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                using SqlCommand command =
                    new SqlCommand(query, connection);

                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@product_id",
                    model.product_id
                );

                command.Parameters.AddWithValue(
                    "@product_name",
                    model.product_name ?? (object)DBNull.Value
                );

                command.Parameters.AddWithValue(
                    "@product_code",
                    model.product_code ?? (object)DBNull.Value
                );

                command.Parameters.AddWithValue(
                    "@product_type",
                    model.product_type ?? (object)DBNull.Value
                );

                command.Parameters.AddWithValue(
                    "@uom_id",
                    model.uom_id
                );

                command.Parameters.AddWithValue(
                    "@category_id",
                    model.category_id
                );

                command.Parameters.AddWithValue(
                    "@subcategory_id",
                    model.subcategory_id
                );

                command.Parameters.AddWithValue(
                    "@updated_at",
                    DateTime.Now
                );

                int result =
                    await command.ExecuteNonQueryAsync();

                return result > 0;
            }
            catch
            {
                return false;
            }
        }

        // DELETE
        public async Task<bool> DeleteMasterProduct(
            int productId
        )
        {
            const string query = @"
                DELETE FROM master_product
                WHERE product_id = @product_id";

            try
            {
                using SqlConnection connection =
                    new SqlConnection(_connectionString);

                using SqlCommand command =
                    new SqlCommand(query, connection);

                await connection.OpenAsync();

                command.Parameters.AddWithValue(
                    "@product_id",
                    productId
                );

                int result =
                    await command.ExecuteNonQueryAsync();

                return result > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}