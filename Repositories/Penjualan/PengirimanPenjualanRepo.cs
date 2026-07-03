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
        Task<List<DeliveryOrderDetailDTO>> GetDoDetail(int deliveryOrderId);

        Task<int> InsertDeliveryOrderHeader(
        DeliveryOrderHeaderDTO dto,
        SqlConnection connection,
        SqlTransaction transaction);
        Task InsertDeliveryOrderDetail(
    DeliveryOrderDetailDTO dto,
    SqlConnection connection,
    SqlTransaction transaction);
        Task UpdateDeliveryOrderHeader(
            DeliveryOrderHeaderDTO dto,
            SqlConnection connection,
            SqlTransaction transaction);
        Task DeleteDeliveryOrderDetail(
            int deliveryOrderId,
            SqlConnection connection,
            SqlTransaction transaction);


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
                            doh.po_number							AS PoNumber,
                            doh.so_id                              AS SoId,
                            so.so_number 							AS SoNumber,
                            doh.delivery_category_id  				AS DeliveryCategoryId,
                            dc.category_name 						AS DeliveryShippingName,
                            doh.address 							AS Address,
                            doh.notes 								AS Notes,
                            ISNULL(doh.status, 'Draft')            AS Status
                            FROM delivery_order_header doh 
                            JOIN master_customer mc ON doh.customer_id = mc.customer_id
                            LEFT JOIN sales_order so  on doh.so_id = so.order_id 
                            LEFT JOIN delivery_category dc on dc.id = doh.delivery_category_id";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<DeliveryOrderHeaderDTO>(query);
            return result.ToList();
        }

        public async Task<List<DeliveryOrderDetailDTO>> GetDoDetail(int deliveryOrderId)
        {
            const string query = @"
                SELECT
                    dod.delivery_id AS DoId,
                    dod.product_id AS ProductId,
                    mp.product_code AS ProductCode,
                    mp.product_name AS ProductName,
                    dod.qty_dikirim AS QtyDikirim,
                    dod.qty_dipesan AS QtyDipesan,
                    sod.uom_id AS UomId,
                    mu.uom_code AS UomName
                FROM delivery_order_detail dod
                LEFT JOIN master_product mp ON mp.product_id = dod.product_id
                LEFT JOIN delivery_order_header doh ON doh.id = dod.delivery_id
                LEFT JOIN sales_order_detail sod
                    ON sod.order_id = doh.so_id
                   AND sod.product_id = dod.product_id
                LEFT JOIN master_uom mu ON mu.uom_id = sod.uom_id
                WHERE dod.delivery_id = @DeliveryOrderId
                ORDER BY dod.product_id ASC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<DeliveryOrderDetailDTO>(
                query,
                new { DeliveryOrderId = deliveryOrderId });

            return result.ToList();
        }


        public async Task<int> InsertDeliveryOrderHeader(
    DeliveryOrderHeaderDTO dto,
    SqlConnection connection,
    SqlTransaction transaction)
        {
            string query = @"
        INSERT INTO delivery_order_header
        (
            customer_id,
            do_number,
            delivery_category_id,
            po_number,
            address,
            notes,
            do_date,
            so_id
        )
        OUTPUT INSERTED.id
        VALUES
        (
            @CustomerId,
            @DoNumber,
            @DeliveryCategoryId,
            @PoNumber,
            @Address,
            @Notes,
            @DoDate,
            @SoId
        )";

            var deliveryOrderId = await connection.ExecuteScalarAsync<int>(
                query,
                dto,
                transaction);

            if (dto.SoId.HasValue && dto.SoId.Value > 0)
            {
                const string updateSalesOrderStatusQuery = @"
                    UPDATE sales_order
                    SET status = 'In Delivery'
                    WHERE order_id = @SalesOrderId;";

                await connection.ExecuteAsync(
                    updateSalesOrderStatusQuery,
                    new { SalesOrderId = dto.SoId.Value },
                    transaction);
            }

            return deliveryOrderId;
        }

        public async Task InsertDeliveryOrderDetail(
    DeliveryOrderDetailDTO dto,
    SqlConnection connection,
    SqlTransaction transaction)
        {
            string query = @"
        INSERT INTO delivery_order_detail
        (
            delivery_id,
            product_id,
            qty_dikirim,
            qty_dipesan
        )
        VALUES
        (
            @DoId,
            @ProductId,
            @QtyDikirim,
            @QtyDipesan
        )";

            await connection.ExecuteAsync(
                query,
                dto,
                transaction);
        }

        public async Task UpdateDeliveryOrderHeader(
            DeliveryOrderHeaderDTO dto,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                UPDATE delivery_order_header
                SET
                    customer_id = @CustomerId,
                    do_number = @DoNumber,
                    delivery_category_id = @DeliveryCategoryId,
                    po_number = @PoNumber,
                    address = @Address,
                    notes = @Notes,
                    do_date = @DoDate,
                    so_id = @SoId
                WHERE id = @Id;";

            await connection.ExecuteAsync(query, dto, transaction);
        }

        public async Task DeleteDeliveryOrderDetail(
            int deliveryOrderId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                DELETE FROM delivery_order_detail
                WHERE delivery_id = @DeliveryOrderId;";

            await connection.ExecuteAsync(
                query,
                new { DeliveryOrderId = deliveryOrderId },
                transaction);
        }
 
    }
}
