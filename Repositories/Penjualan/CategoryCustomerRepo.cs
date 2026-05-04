
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Reflection;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ICategoryCustomerRepo
    {
        // Insert Data 
        Task<bool> InsertCategoryCust(CategoryCustomer model);
        //Task<bool> InsertCust(Customer model);

        Task<List<CategoryCustomer>> GetAllCategory();
        Task<bool> UpdateCategoryCust(CategoryCustomer model);

        Task<bool> UpdateStatusCategory(int id, int status);
    }
    public class CategoryCustomerRepo : ICategoryCustomerRepo
    {
        private readonly string _connectionString;
        public CategoryCustomerRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

      

        public async Task<bool> InsertCategoryCust(CategoryCustomer model)
        {

            const string query = @"INSERT INTO master_customer_category (category_name) 
                                   VALUES (@category_name)";

            try
            {
                using(SqlConnection connection = new SqlConnection(_connectionString))
                using(SqlCommand command = new SqlCommand(query,connection))
                {
                    await connection.OpenAsync();
                    command.Parameters.AddWithValue("@category_name", model.NamaKategori);
                    int result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                return false;
                Console.WriteLine(ex.Message);
            }
        }

        public async Task<bool> UpdateCategoryCust(CategoryCustomer model)
        {
            const string query = @"UPDATE master_customer_category 
                                   SET category_name = @categoryName
                                    WHERE id = @id";
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    command.Parameters.AddWithValue("@categoryName", model.NamaKategori);
                    command.Parameters.AddWithValue("@id", model.Id);
                    int result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
                                
        }

        public async Task<bool> UpdateStatusCategory(int id, int status)
        {
            const string query = "UPDATE master_customer_category SET is_active =  @status WHERE id = @id";
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    command.Parameters.AddWithValue("@status",status);
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

        public async Task<List<CategoryCustomer>> GetAllCategory()
        {
            const string query = @"SELECT * FROM master_customer_category";
            var response = new List<CategoryCustomer>();
            try
            {
                using(SqlConnection connection = new SqlConnection(_connectionString))
                    using(SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    using(SqlDataReader reader = await  command.ExecuteReaderAsync())
                    {
                        
                        while (await reader.ReadAsync())
                        {
                            var category = new CategoryCustomer() {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                NamaKategori = reader.GetString(reader.GetOrdinal("category_name"))
                            };

                            response.Add(category);
                        }
                       
                    }   
                }
            }catch(Exception ex)
            {
                var msg = ex.Message;
                throw new Exception(msg);
            }
            return response;
        }
    }
}
