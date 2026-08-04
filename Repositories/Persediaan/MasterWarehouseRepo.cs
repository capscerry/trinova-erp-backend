using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterWarehouseRepo
    {
        private readonly string _connectionString;

        public MasterWarehouseRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        // GET ALL
        public async Task<List<MasterWarehouse>> GetAllMasterWarehouse()
        {
            var response = new List<MasterWarehouse>();

            const string query = @"
                SELECT *
                FROM master_warehouse
                WHERE is_active = 1";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(new MasterWarehouse
                {
                    warehouse_id = Convert.ToInt32(reader["warehouse_id"]),
                    warehouse_name = reader["warehouse_name"].ToString(),
                    description = reader["description"].ToString(),
                    warehouse_address = reader["warehouse_address"].ToString(),
                    warehouse_type = reader["warehouse_type"].ToString(),
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
        public async Task<MasterWarehouse?> GetMasterWarehouseById(int warehouseId)
        {
            MasterWarehouse? response = null;

            const string query = @"
                SELECT *
                FROM master_warehouse
                WHERE warehouse_id = @warehouse_id
                AND is_active = 1";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@warehouse_id", warehouseId);

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                response = new MasterWarehouse
                {
                    warehouse_id = Convert.ToInt32(reader["warehouse_id"]),
                    warehouse_name = reader["warehouse_name"].ToString(),
                    description = reader["description"].ToString(),
                    warehouse_address = reader["warehouse_address"].ToString(),
                    warehouse_type = reader["warehouse_type"].ToString(),
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
        public async Task<bool> InsertMasterWarehouse(MasterWarehouse model)
        {
            const string query = @"
                INSERT INTO master_warehouse
                (
                    warehouse_name,
                    description,
                    warehouse_address,
                    warehouse_type,
                    created_at,
                    created_by,
                    updated_at,
                    updated_by,
                    is_active
                )
                VALUES
                (
                    @warehouse_name,
                    @description,
                    @warehouse_address,
                    @warehouse_type,
                    @created_at,
                    @created_by,
                    @updated_at,
                    @updated_by,
                    @is_active
                )";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@warehouse_name", model.warehouse_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@description", model.description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@warehouse_address", model.warehouse_address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@warehouse_type", model.warehouse_type ?? (object)DBNull.Value);
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
        public async Task<bool> UpdateMasterWarehouse(MasterWarehouse model)
        {
            const string query = @"
                UPDATE master_warehouse
                SET
                    warehouse_name = @warehouse_name,
                    description = @description,
                    warehouse_address = @warehouse_address,
                    warehouse_type = @warehouse_type,
                    updated_at = @updated_at,
                    updated_by = @updated_by
                WHERE warehouse_id = @warehouse_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@warehouse_id", model.warehouse_id);
            command.Parameters.AddWithValue("@warehouse_name", model.warehouse_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@description", model.description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@warehouse_address", model.warehouse_address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@warehouse_type", model.warehouse_type ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.AddHours(7));
            command.Parameters.AddWithValue("@updated_by", model.updated_by ?? "system");

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        // SOFT DELETE
        public async Task<bool> DeleteMasterWarehouse(int warehouseId)
        {
            const string query = @"
                UPDATE master_warehouse
                SET
                    is_active = 0,
                    updated_at = @updated_at
                WHERE warehouse_id = @warehouse_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@warehouse_id", warehouseId);
            command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow.AddHours(7));

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }
    }
}