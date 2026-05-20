// Repositories/Persediaan/MasterProductRepo.cs

using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterProductRepo
    {
        private readonly string _connectionString;

        public MasterProductRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }


        public async Task<List<ProductDTO>> GetAllProduct()
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                SELECT
                    mp.product_id      AS ProductId,
                    mp.product_code    AS ProductCode,
                    mp.product_name    AS ProductName,
                    mp.product_type    AS ProductType,
                    mpc.category_id    AS CategoryId,
                    mpc.category_name  AS CategoryName,
                    mu.uom_code        AS Uom
                FROM master_product mp
                JOIN master_product_category mpc
                    ON mp.category_id = mpc.category_id
                JOIN master_uom mu
                    ON mp.uom_id = mu.uom_id
                ORDER BY mp.product_name
            ";

            var result = await connection.QueryAsync<ProductDTO>(query);

            return result.ToList();
        }
        // GET ALL
        public async Task<List<MasterProduct>> GetAllMasterProduct()
        {
            var response = new List<MasterProduct>();

            const string query = @"
                SELECT *
                FROM master_product";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                response.Add(new MasterProduct
                {
                    product_id = Convert.ToInt32(reader["product_id"]),
                    product_name = reader["product_name"].ToString(),
                    product_code = reader["product_code"].ToString(),
                    product_type = reader["product_type"].ToString(),
                    barcode = reader["barcode"].ToString(),
                    uom_id = Convert.ToInt32(reader["uom_id"]),
                    category_id = Convert.ToInt32(reader["category_id"]),
                    created_at = Convert.ToDateTime(reader["created_at"]),
                    updated_at = Convert.ToDateTime(reader["updated_at"])
                });
            }

            return response;
        }

        // GET BY ID
        public async Task<MasterProduct?> GetMasterProductById(int productId)
        {
            MasterProduct? response = null;

            const string query = @"
                SELECT *
                FROM master_product
                WHERE product_id = @product_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@product_id", productId);

            await connection.OpenAsync();

            using SqlDataReader reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                response = new MasterProduct
                {
                    product_id = Convert.ToInt32(reader["product_id"]),
                    product_name = reader["product_name"].ToString(),
                    product_code = reader["product_code"].ToString(),
                    product_type = reader["product_type"].ToString(),
                    barcode = reader["barcode"].ToString(),
                    uom_id = Convert.ToInt32(reader["uom_id"]),
                    category_id = Convert.ToInt32(reader["category_id"]),
                    created_at = Convert.ToDateTime(reader["created_at"]),
                    updated_at = Convert.ToDateTime(reader["updated_at"])
                };
            }

            return response;
        }

        // INSERT
        public async Task<bool> InsertMasterProduct(MasterProduct model)
        {
            const string query = @"
                INSERT INTO master_product
                (
                    product_name,
                    product_code,
                    product_type,
                    barcode,
                    uom_id,
                    category_id,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @product_name,
                    @product_code,
                    @product_type,
                    @barcode,
                    @uom_id,
                    @category_id,
                    @created_at,
                    @updated_at
                )";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@product_name", model.product_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@product_code", model.product_code ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@product_type", model.product_type ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@barcode", model.barcode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@uom_id", model.uom_id);
            command.Parameters.AddWithValue("@category_id", model.category_id);
            command.Parameters.AddWithValue("@created_at", model.created_at ?? DateTime.Now);
            command.Parameters.AddWithValue("@updated_at", model.updated_at ?? DateTime.Now);

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        // UPDATE
        public async Task<bool> UpdateMasterProduct(MasterProduct model)
        {
            const string query = @"
                UPDATE master_product
                SET
                    product_name = @product_name,
                    product_code = @product_code,
                    product_type = @product_type,
                    barcode = @barcode,
                    uom_id = @uom_id,
                    category_id = @category_id,
                    updated_at = @updated_at
                WHERE product_id = @product_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@product_id", model.product_id);
            command.Parameters.AddWithValue("@product_name", model.product_name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@product_code", model.product_code ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@product_type", model.product_type ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@barcode", model.barcode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@uom_id", model.uom_id);
            command.Parameters.AddWithValue("@category_id", model.category_id);
            command.Parameters.AddWithValue("@updated_at", DateTime.Now);

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }

        // DELETE
        public async Task<bool> DeleteMasterProduct(int productId)
        {
            const string query = @"
                DELETE FROM master_product
                WHERE product_id = @product_id";

            using SqlConnection connection = new SqlConnection(_connectionString);
            using SqlCommand command = new SqlCommand(query, connection);

            command.Parameters.AddWithValue("@product_id", productId);

            await connection.OpenAsync();

            int result = await command.ExecuteNonQueryAsync();

            return result > 0;
        }
    }
}