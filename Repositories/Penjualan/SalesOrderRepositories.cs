using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesOrderRepositories
    {
        Task<SalesOrderHeader> InsertSalesOrderHeader(
            SalesOrderHeader header,
            IDbConnection connection,
            IDbTransaction tx
        );

        Task<SalesOrderDetail> InsertSalesOrderDetail(
            SalesOrderDetail detail,
            IDbConnection connection,
            IDbTransaction tx
        );

        Task<List<SalesOrderHeader>> GetAllSalesOrder();

        Task<SalesOrderDetailDTO> GetSalesOrderDetail(int orderId);
    }

    public class SalesOrderRepositories : ISalesOrderRepositories
    {
        private readonly string _connectionString;

        public SalesOrderRepositories(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer;
        }

        public async Task<SalesOrderHeader> InsertSalesOrderHeader(
            SalesOrderHeader header,
            IDbConnection connection,
            IDbTransaction tx
        )
        {
            string query = @"
    INSERT INTO sales_order
    (
        so_number,
        tanggal_kirim,
        so_date,
        po_number,
        subtotal,
        customer_id,
        is_taxable,
        is_tax_included,
        address,
        notes
    )
    OUTPUT
        INSERTED.order_id AS OrderId,
        INSERTED.customer_id AS CustomerId,
        INSERTED.so_number AS SoNumber,
        INSERTED.tanggal_kirim AS TanggalKirim,
        INSERTED.so_date AS SoDate,
        INSERTED.po_number AS PoNumber,
        INSERTED.subtotal AS SubTotal,
        INSERTED.is_taxable AS IsTaxAble,
        INSERTED.is_tax_included AS IsTaxIncluded,
        INSERTED.address AS Address,
        INSERTED.notes AS Notes
    VALUES
    (
        @SoNumber,
        @TanggalKirim,
        @SoDate,
        @PoNumber,
        @SubTotal,
        @CustomerId,
        @IsTaxAble,
        @IsTaxIncluded,
        @Address,
        @Notes
    );
";

            return await connection.QuerySingleAsync<SalesOrderHeader>(
                query,
                header,
                tx
            );
        }

        public async Task<SalesOrderDetail> InsertSalesOrderDetail(
    SalesOrderDetail detail,
    IDbConnection connection,
    IDbTransaction tx
)
        {
            string query = @"
    INSERT INTO sales_order_detail
    (
        order_id,
        product_id,
        product_code,
        product_name,
        product_qty,
        product_price,
        discount_amount,
        total_price,
        warehouse_id
    )
    OUTPUT
        INSERTED.order_id AS OrderId,
        INSERTED.product_id AS ProductId,
        INSERTED.product_code AS ProductCode,
        INSERTED.product_name AS ProductName,
        INSERTED.product_qty AS ProductQty,
        INSERTED.product_price AS ProductPrice,
        INSERTED.discount_amount AS DiscountAmount,
        INSERTED.total_price AS TotalPrice,
        INSERTED.warehouse_id AS WarehouseId
    VALUES
    (
        @OrderId,
        @ProductId,
        @ProductCode,
        @ProductName,
        @ProductQty,
        @ProductPrice,
        @DiscountAmount,
        @TotalPrice,
        @WarehouseId
    );
";

            return await connection.QuerySingleAsync<SalesOrderDetail>(
                query,
                detail,
                tx
            );
        }

        public async Task<List<SalesOrderHeader>> GetAllSalesOrder()
        {
            string query = @"         
                 SELECT
                     so.order_id        AS OrderId,
                     so.customer_id     AS CustomerId,         
                     mc.customer_name   AS CustomerName,
                     so.so_number       AS SoNumber,
                     so.tanggal_kirim   AS TanggalKirim,
                     so.so_date         AS SoDate,
                     so.po_number       AS PoNumber,
                     so.subtotal        AS SubTotal,
                     so.is_taxable      AS IsTaxAble,
                     so.is_tax_included AS IsTaxIncluded,
                     so.address         AS Address,
                     so.notes           AS Notes
                 FROM sales_order so JOIN master_customer mc  ON so.customer_id  = mc.customer_id 
                 ORDER BY order_id DESC;
            ";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<SalesOrderHeader>(query);

            return result.ToList();
        }

        public async Task<SalesOrderDetailDTO> GetSalesOrderDetail(int orderId)
        {
            using var connection = new SqlConnection(_connectionString);

            string headerQuery = @"
                SELECT 
                    so.so_number AS SoNumber,
                    mc.customer_name AS CustomerName,
                    so.so_date AS SoDate,
                    so.tanggal_kirim AS TanggalKirim,
                    so.po_number AS PoNumber,
                    so.address AS Address,
                    so.subtotal AS Total,
                    so.notes AS Keterangan
                FROM sales_order so
                JOIN master_customer mc 
                    ON mc.customer_id = so.customer_id
                WHERE so.order_id = @OrderId
            ";

            string detailQuery = @"
                SELECT 
                    mp.product_name AS ProductName,
                    sod.product_qty AS ProductQty,
                    sod.product_price AS ProductPrice,
                    sod.discount_amount AS ProductDiscount,
                    sod.total_price AS TotalPrice
                FROM sales_order_detail sod
                JOIN master_product mp 
                    ON mp.product_id = sod.product_id
                WHERE sod.order_id = @OrderId
            ";

            var header = await connection.QueryFirstOrDefaultAsync<SalesOrderDetailDTO>(
                headerQuery,
                new { OrderId = orderId }
            );

            if (header == null)
                return null;

            var details = await connection.QueryAsync<SalesOrderProductDetail>(
                detailQuery,
                new { OrderId = orderId }
            );

            header.Detail = details.ToList();

            return header;


        }
    }
}