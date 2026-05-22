using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using trinova_erp_backend.Config;
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
            customer_id,
            is_taxable,
            is_tax_included,
            address,
            notes
        )
        OUTPUT INSERTED.*
        VALUES
        (
            @SoNumber,
            @TanggalKirim,
            @SoDate,
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
        OUTPUT INSERTED.*
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
            @WareHouseId
        );
    ";

            return await connection.QuerySingleAsync<SalesOrderDetail>(
                query,
                detail,
                tx
            );
        }
    }
}