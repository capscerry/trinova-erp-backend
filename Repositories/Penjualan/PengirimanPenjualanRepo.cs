using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface IPengirimanPenjualanRepo
    {
        Task<List<ShippingDTO>> GetShippingCategory();
        Task<List<DeliveryOrderHeaderDTO>> GetDoHeader();


    }
    public class PengirimanPenjualanRepo : IPengirimanPenjualanRepo
    {
        private readonly string _connectionString;
        public PengirimanPenjualanRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer;
        }
        
        public async Task<List<ShippingDTO>> GetShippingCategory()
        {
            const string query = @"SELECT 
                                    id AS Id,
                                    category_name AS ShippingName 
                                   FROM delivery_category";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<ShippingDTO>(query);

            return result.ToList();
        }

        public async Task<List<DeliveryOrderHeaderDTO>> GetDoHeader()
        {
            string query = @"SELECT
                            doh.id 									AS Id,
                            doh.customer_id 						AS CustomerId,
                            doh.do_date     						AS DoDate,
                            mc.customer_name 						AS CustomerName,
                            doh.do_number							AS DoNumber,
                            so.so_number 							AS SoNumber,
                            doh.delivery_category_id  				AS DeliveryCategoryId,
                            dc.category_name 						AS DeliveryShippingName,
                            doh.address 							AS Address,
                            doh.notes 								AS Notes
                            FROM delivery_order_header doh 
                            JOIN master_customer mc ON doh.customer_id = mc.customer_id
                            LEFT JOIN sales_order so  on doh.so_id = so.order_id 
                            LEFT JOIN delivery_category dc on dc.id = doh.delivery_category_id";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<DeliveryOrderHeaderDTO>(query);
            return result.ToList();
        }

        //public async Task<List<DeliveryOrderHeaderDTO>> GetDoDetailById(int id)
        //{
        //    string query = @""
        //}
    }
}
