using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public interface IMasterUomRepo
    {
        Task<List<MasterUom>> GetAllMasterUom();
    }

    public class MasterUomRepo : IMasterUomRepo
    {
        private readonly string _connectionString;

        public MasterUomRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        public async Task<List<MasterUom>> GetAllMasterUom()
        {
            const string query = @"SELECT * FROM master_uom";

            var response = new List<MasterUom>();

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
                            var uom = new MasterUom()
                            {
                                uom_id = reader.GetInt32(
                                    reader.GetOrdinal("uom_id")
                                ),

                                uom_code = reader["uom_code"].ToString(),

                                uom_name = reader["uom_name"].ToString()
                            };

                            response.Add(uom);
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