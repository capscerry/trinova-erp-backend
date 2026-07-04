// Repositories/Persediaan/MasterProductRepo.cs

using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;
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
            _connectionString = options.Value.SQLServer!;
        }

        // =========================================================
        // GET ALL PRODUCT DTO
        // =========================================================
        public async Task<List<ProductDTO>> GetAllProduct()
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                SELECT
                    mp.product_id      AS ProductId,
                    mp.product_code    AS ProductCode,
                    mp.product_name    AS ProductName,
                    mpc.category_id    AS CategoryId,
                    mpc.category_name  AS CategoryName,
                    mu.uom_code        AS Uom,
                    mu.uom_id          AS UomId
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

        // =========================================================
        // GET ALL MASTER PRODUCT
        // =========================================================
        //public async Task<List<MasterProduct>> GetAllMasterProduct()
        //{
        //    var response = new List<MasterProduct>();

        //    const string query = @"
        //        SELECT *
        //        FROM master_product
        //        ORDER BY product_name";

        //    using SqlConnection connection = new SqlConnection(_connectionString);
        //    using SqlCommand command = new SqlCommand(query, connection);

        //    await connection.OpenAsync();

        //    using SqlDataReader reader = await command.ExecuteReaderAsync();

        //    while (await reader.ReadAsync())
        //    {
        //        response.Add(new MasterProduct
        //        {
        //            product_id = Convert.ToInt32(reader["product_id"]),
        //            product_name = reader["product_name"]?.ToString(),
        //            product_code = reader["product_code"]?.ToString(),
        //            product_type = reader["product_type"]?.ToString(),
        //            //barcode = reader["barcode"]?.ToString(),
        //            uom_id = Convert.ToInt32(reader["uom_id"]),
        //            category_id = Convert.ToInt32(reader["category_id"]),
        //            created_at = Convert.ToDateTime(reader["created_at"]),
        //            updated_at = Convert.ToDateTime(reader["updated_at"])
        //        });
        //    }

        //    return response;
        //}

        // =========================================================
        // GET BY ID
        // =========================================================
        //public async Task<MasterProduct?> GetMasterProductById(int productId)
        //{
        //    MasterProduct? response = null;

        //    const string query = @"
        //        SELECT *
        //        FROM master_product
        //        WHERE product_id = @product_id";

        //    using SqlConnection connection = new SqlConnection(_connectionString);
        //    using SqlCommand command = new SqlCommand(query, connection);

        //    command.Parameters.AddWithValue("@product_id", productId);

        //    await connection.OpenAsync();

        //    using SqlDataReader reader = await command.ExecuteReaderAsync();

        //    if (await reader.ReadAsync())
        //    {
        //        response = new MasterProduct
        //        {
        //            product_id = Convert.ToInt32(reader["product_id"]),
        //            product_name = reader["product_name"]?.ToString(),
        //            product_code = reader["product_code"]?.ToString(),
        //            product_type = reader["product_type"]?.ToString(),
        //            //barcode = reader["barcode"]?.ToString(),
        //            uom_id = Convert.ToInt32(reader["uom_id"]),
        //            category_id = Convert.ToInt32(reader["category_id"]),
        //            created_at = Convert.ToDateTime(reader["created_at"]),
        //            updated_at = Convert.ToDateTime(reader["updated_at"])
        //        };
        //    }

        //    return response;
        //}

        // =========================================================
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

                Console.WriteLine(
                    $"INSERT PRODUCT => " +
                    $"NAME={model.product_name}, " +
                    $"CATEGORY={model.category_id}, " +
                    $"SUBCATEGORY={model.subcategory_id}, " +
                    $"UOM={model.uom_id}"
                );

                int result =
                    await command.ExecuteNonQueryAsync();

                Console.WriteLine(
                    $"ROWS AFFECTED = {result}"
                );

                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "===== INSERT PRODUCT ERROR ====="
                );

                Console.WriteLine(
                    ex.ToString()
                );

                throw;
            }
        }

        // GET ALL
        public async Task<List<MasterProduct>>
            GetAllMasterProduct()
        {
            const string query = @"
            SELECT
                p.*,

                u.uom_code,
                u.uom_name,

                c.category_name,

                s.subcategory_code,
                s.subcategory_name,
                s.is_active AS subcategory_is_active

            FROM master_product p

            LEFT JOIN master_uom u
                ON p.uom_id = u.uom_id

            LEFT JOIN master_product_category c
                ON p.category_id = c.category_id

            LEFT JOIN master_product_subcategory s
                ON p.subcategory_id = s.subcategory_id

            ORDER BY p.product_id DESC";

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
                                    ),

                            MasterUom =
                                reader["uom_name"] == DBNull.Value
                                ? null
                                : new MasterUom
                                {
                                    uom_id = Convert.ToInt32(
                                        reader["uom_id"]
                                    ),

                                    uom_code =
                                        reader["uom_code"]
                                            ?.ToString(),

                                    uom_name =
                                        reader["uom_name"]
                                            ?.ToString()
                                },

                            MasterProductCategory =
                                reader["category_name"] == DBNull.Value
                                ? null
                                : new MasterProductCategory
                                {
                                    category_id =
                                        Convert.ToInt32(
                                            reader["category_id"]
                                        ),

                                    category_name =
                                        reader["category_name"]
                                            ?.ToString()
                                },

                            ProductSubcategory =
                                reader["subcategory_name"] == DBNull.Value
                                ? null
                                : new ProductSubcategory
                                {
                                    subcategory_id =
                                        Convert.ToInt32(
                                            reader["subcategory_id"]
                                        ),

                                    category_id =
                                        Convert.ToInt32(
                                            reader["category_id"]
                                        ),

                                    code =
                                        reader["subcategory_code"]
                                            ?.ToString()
                                            ?? "",

                                    name =
                                        reader["subcategory_name"]
                                            ?.ToString()
                                            ?? "",

                                    is_active =
                                        reader["subcategory_is_active"]
                                            != DBNull.Value
                                        &&
                                        Convert.ToBoolean(
                                            reader["subcategory_is_active"]
                                        )
                                }          
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

        // =========================================================
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

        // =========================================================
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