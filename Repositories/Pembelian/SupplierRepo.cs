using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface ISupplierRepo
    {
        Task<bool> InsertSupplier(Supplier model);

        Task<List<Supplier>> GetAllSupplier();

        Task<Supplier?> GetSupplierById(int id);

        Task<bool> UpdateSupplier(Supplier model);

        Task<bool> DeleteSupplier(int id);
    }

    public class SupplierRepo : ISupplierRepo
    {
        private readonly string _connectionString;

        public SupplierRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        // INSERT
        public async Task<bool> InsertSupplier(Supplier model)
        {
            const string query = @"
                INSERT INTO master_supplier
                (
                    supplier_code,
                    supplier_name,
                    category_supplier,
                    alamat,
                    email,
                    status
                )
                VALUES
                (
                    @supplier_code,
                    @supplier_name,
                    @category_supplier,
                    @alamat,
                    @email,
                    @status
                )";

            try
            {
                using (
                    SqlConnection connection = new SqlConnection(
                        _connectionString
                    )
                )
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@supplier_code",
                        model.supplier_code
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_name",
                        model.supplier_name
                    );

                    command.Parameters.AddWithValue(
                        "@category_supplier",
                        model.category_supplier
                    );

                    command.Parameters.AddWithValue(
                        "@alamat",
                        model.alamat
                    );

                    command.Parameters.AddWithValue(
                        "@email",
                        model.email
                    );

                    command.Parameters.AddWithValue(
                        "@status",
                        model.status
                    );

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
        public async Task<bool> UpdateSupplier(Supplier model)
        {
            const string query = @"
                UPDATE master_supplier
                SET
                    supplier_code = @supplier_code,
                    supplier_name = @supplier_name,
                    category_supplier = @category_supplier,
                    alamat = @alamat,
                    email = @email,
                    status = @status
                WHERE supplier_id = @supplier_id";

            try
            {
                using (
                    SqlConnection connection = new SqlConnection(
                        _connectionString
                    )
                )
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue(
                        "@supplier_id",
                        model.supplier_id
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_code",
                        model.supplier_code
                    );

                    command.Parameters.AddWithValue(
                        "@supplier_name",
                        model.supplier_name
                    );

                    command.Parameters.AddWithValue(
                        "@category_supplier",
                        model.category_supplier
                    );

                    command.Parameters.AddWithValue(
                        "@alamat",
                        model.alamat
                    );

                    command.Parameters.AddWithValue(
                        "@email",
                        model.email
                    );

                    command.Parameters.AddWithValue(
                        "@status",
                        model.status
                    );

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
        public async Task<bool> DeleteSupplier(int id)
        {
            const string query = @"
                DELETE FROM master_supplier
                WHERE supplier_id = @id";

            try
            {
                using (
                    SqlConnection connection = new SqlConnection(
                        _connectionString
                    )
                )
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
        public async Task<List<Supplier>> GetAllSupplier()
        {
            const string query = @"SELECT * FROM master_supplier";

            var response = new List<Supplier>();

            try
            {
                using (
                    SqlConnection connection = new SqlConnection(
                        _connectionString
                    )
                )
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    using (
                        SqlDataReader reader =
                            await command.ExecuteReaderAsync()
                    )
                    {
                        while (await reader.ReadAsync())
                        {
                            var supplier = new Supplier()
                            {
                                supplier_id = reader.GetInt32(
                                    reader.GetOrdinal("supplier_id")
                                ),

                                supplier_code = reader.GetString(
                                    reader.GetOrdinal("supplier_code")
                                ),

                                supplier_name = reader.GetString(
                                    reader.GetOrdinal("supplier_name")
                                ),

                                category_supplier = reader.GetInt32(
                                    reader.GetOrdinal("category_supplier")
                                ),

                                alamat = reader["alamat"].ToString(),

                                email = reader["email"].ToString(),

                                status = reader["status"].ToString()
                            };

                            response.Add(supplier);
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

        // GET BY ID
        public async Task<Supplier?> GetSupplierById(int id)
        {
            const string query = @"
                SELECT * FROM master_supplier
                WHERE supplier_id = @id";

            try
            {
                using (
                    SqlConnection connection = new SqlConnection(
                        _connectionString
                    )
                )
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    await connection.OpenAsync();

                    command.Parameters.AddWithValue("@id", id);

                    using (
                        SqlDataReader reader =
                            await command.ExecuteReaderAsync()
                    )
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Supplier()
                            {
                                supplier_id = reader.GetInt32(
                                    reader.GetOrdinal("supplier_id")
                                ),

                                supplier_code = reader["supplier_code"]
                                    .ToString(),

                                supplier_name = reader["supplier_name"]
                                    .ToString(),

                                category_supplier = reader.GetInt32(
                                    reader.GetOrdinal("category_supplier")
                                ),

                                alamat = reader["alamat"].ToString(),

                                email = reader["email"].ToString(),

                                status = reader["status"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw new Exception(msg);
            }

            return null;
        }
    }
}