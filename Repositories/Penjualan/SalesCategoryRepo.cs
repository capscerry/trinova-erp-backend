
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Reflection;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesCategoryRepo
    {
        // Insert Data 
        Task<bool> InsertCategorySales(SalesCategory model);
        //Task<bool> InsertCust(Customer model);
        Task<List<SalesCategory>> GetAllCategory();

        Task<bool> UpdateCategorySales(SalesCategory model);

        Task<bool> UpdateStatusCategory(int id, int status);
    }
    public class SalesCategoryRepo : ISalesCategoryRepo
    {
        private readonly string _connectionString;
        public SalesCategoryRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

      


        public async Task<bool> InsertCategorySales(SalesCategory model)
        {

            const string query = @"INSERT INTO sales_category (category_name,keterangan,is_active) 
                                   VALUES (@category_name,@keterangan,1)";

            try
            {
                using(SqlConnection connection = new SqlConnection(_connectionString))
                using(SqlCommand command = new SqlCommand(query,connection))
                {
                    await connection.OpenAsync();
                    command.Parameters.AddWithValue("@category_name", model.NamaKategori);
                    command.Parameters.AddWithValue("@keterangan", model.Keterangan);   
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



        public async Task<bool> UpdateCategorySales(SalesCategory model)
        {
            const string query = @"UPDATE sales_category 
                                   SET category_name = @categoryName,
                                       keterangan = @keterangan
                                    WHERE id = @id";
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();
                    command.Parameters.AddWithValue("@categoryName", model.NamaKategori);
                    command.Parameters.AddWithValue("@keterangan", model.Keterangan ?? string.Empty);
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
            const string query = "UPDATE sales_category SET is_active =  @status WHERE id = @id";
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


        public async Task<List<SalesCategory>> GetAllCategory()
        {
            const string query = @"SELECT * FROM sales_category";
            var response = new List<SalesCategory>();
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
                            var category = new SalesCategory() {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                NamaKategori = reader.GetString(reader.GetOrdinal("category_name")),
                                Keterangan = reader["keterangan"] == DBNull.Value ? string.Empty : reader.GetString(reader.GetOrdinal("keterangan")),
                                IsActive = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"])
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
