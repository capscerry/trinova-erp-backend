using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface ISupplierCategoryRepo
    {
        Task<bool> InsertSupplierCategory(SupplierCategory model);

        Task<List<SupplierCategory>> GetAllSupplierCategory();

        Task<bool> UpdateSupplierCategory(SupplierCategory model);

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

        // INSERT
        public async Task<bool> InsertSupplierCategory(SupplierCategory model)
        {
            const string query = @"
                INSERT INTO supplier_category
                (
                    category_name,
                    created_by,
                    created_date
                )
                VALUES
                (
                    @category_name,
                    @created_by,
                    GETDATE()
                )";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@category_name", model.category_name);
                    command.Parameters.AddWithValue("@created_by", model.created_by);

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // UPDATE
        public async Task<bool> UpdateSupplierCategory(SupplierCategory model)
        {
            const string query = @"
                UPDATE supplier_category
                SET
                    category_name = @category_name,
                    update_by = @update_by,
                    update_date = GETDATE()
                WHERE category_id = @category_id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@category_id", model.category_id);
                    command.Parameters.AddWithValue("@category_name", model.category_name);
                    command.Parameters.AddWithValue("@update_by", model.update_by);

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // DELETE
        public async Task<bool> DeleteSupplierCategory(int id)
        {
            const string query = @"
                DELETE FROM supplier_category
                WHERE category_id = @id";

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@id", id);

                    int result = await command.ExecuteNonQueryAsync();

                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // GET ALL
        public async Task<List<SupplierCategory>> GetAllSupplierCategory()
        {
            const string query = @"SELECT * FROM supplier_category";

            var response = new List<SupplierCategory>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var category = new SupplierCategory()
                            {
                                category_id = reader.GetInt32(reader.GetOrdinal("category_id")),
                                category_name = reader.GetString(reader.GetOrdinal("category_name")),
                                created_date = reader["created_date"] as DateTime?,
                                created_by = reader["created_by"].ToString(),
                                update_date = reader["update_date"] as DateTime?,
                                update_by = reader["update_by"].ToString()
                            };

                            response.Add(category);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw new Exception(msg);
            }

            return response;
        }
    }
}