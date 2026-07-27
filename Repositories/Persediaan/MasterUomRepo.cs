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
                        // Performance: cache ordinals once before the loop — avoids
                        // a linear string scan on every row for every column access.
                        // Mapping behaviour and returned model are unchanged.
                        int ord_uom_id   = reader.GetOrdinal("uom_id");
                        int ord_uom_code = reader.GetOrdinal("uom_code");
                        int ord_uom_name = reader.GetOrdinal("uom_name");

                        while (await reader.ReadAsync())
                        {
                            var uom = new MasterUom()
                            {
                                uom_id   = reader.GetInt32(ord_uom_id),
                                uom_code = reader[ord_uom_code].ToString(),
                                uom_name = reader[ord_uom_name].ToString()
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