using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;
using Dapper;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface IMasterCustomerRepo {
        Task<bool> InsertMasterCustomer(Customer customer);
        Task<List<Customer>> GetAllCustomer();
    }

    public class MasterCustomerRepo : IMasterCustomerRepo
    {
        private readonly string _connectionString;
        public MasterCustomerRepo(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer ?? throw new InvalidOperationException("Database connection string is not configured.");
        }

        public async Task<List<Customer>> GetAllCustomer()
        {

            string query = @"
                SELECT 
                    c.customer_id AS CustomerId,
                    c.customer_name AS CustomerName,
                    c.customer_code AS CustomerCode,
                    c.no_telp_bisnis AS NoTelpBisnis,
                    c.alamat AS Alamat,
                    c.email AS Email,
                    c.is_active AS IsActive,
                    c.created_date AS CreatedDate,
                    c.update_date AS UpdateDate,
                    cat.category_name AS CategoryName
                FROM master_customer c
                LEFT JOIN master_customer_category cat 
                    ON c.category_id = cat.id
            ";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var result = await connection.QueryAsync<Customer>(query);

                return result.ToList();
            }
        }



        // Insert Customer
        public async Task<bool> InsertMasterCustomer(Customer customer)
        {
            string query = @"
                        INSERT INTO master_customer
                        (customer_name, customer_code, no_telp_bisnis, alamat, email, category_id, is_active)
                        VALUES
                        (@CustomerName, @CustomerCode, @NoTelp, @Alamat, @Email, @CategoryId, @IsActive);
                    ";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var result = await connection.ExecuteAsync(query, new
                {
                    CustomerName = customer.CustomerName,
                    CustomerCode = customer.CustomerCode,
                    NoTelp = customer.NoTelpBisnis,
                    Alamat = customer.Alamat,
                    Email = customer.Email,
                    CategoryId = customer.CategoryId,
                    IsActive = customer.IsActive,
                });

                return result > 0;
            }
        }
    }
}
