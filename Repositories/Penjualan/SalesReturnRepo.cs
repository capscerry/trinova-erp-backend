using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesReturnRepo
    {
        Task<List<SalesReturnHeaderDTO>> GetAllAsync();
        Task<SalesReturn?> GetDetailAsync(int id);
        Task<Dictionary<int, decimal>> GetReturnableQtyAsync(
            int deliveryOrderId,
            SqlConnection connection,
            SqlTransaction transaction);
        Task<Dictionary<int, decimal>> GetReturnableQtyAsync(int deliveryOrderId);
        Task<int> InsertHeaderAsync(
            SalesReturnHeaderDTO header,
            SqlConnection connection,
            SqlTransaction transaction);
        Task InsertDetailAsync(
            SalesReturnDetailDTO detail,
            SqlConnection connection,
            SqlTransaction transaction);
    }

    public class SalesReturnRepo : ISalesReturnRepo
    {
        private readonly string _connectionString;

        public SalesReturnRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer;
        }

        public async Task<List<SalesReturnHeaderDTO>> GetAllAsync()
        {
            const string query = @"
                SELECT
                    srh.id                AS Id,
                    srh.return_number     AS ReturnNumber,
                    srh.return_date       AS ReturnDate,
                    srh.customer_id       AS CustomerId,
                    mc.customer_name      AS CustomerName,
                    srh.delivery_order_id AS DeliveryOrderId,
                    doh.do_number         AS DoNumber,
                    srh.sales_order_id    AS SalesOrderId,
                    so.so_number          AS SoNumber,
                    srh.notes             AS Notes,
                    srh.status            AS Status,
                    srh.created_at        AS CreatedAt
                FROM sales_return_header srh
                JOIN master_customer mc ON mc.customer_id = srh.customer_id
                LEFT JOIN delivery_order_header doh ON doh.id = srh.delivery_order_id
                LEFT JOIN sales_order so ON so.order_id = srh.sales_order_id
                ORDER BY srh.id DESC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<SalesReturnHeaderDTO>(query);
            return result.ToList();
        }

        public async Task<SalesReturn?> GetDetailAsync(int id)
        {
            const string headerQuery = @"
                SELECT
                    srh.id                AS Id,
                    srh.return_number     AS ReturnNumber,
                    srh.return_date       AS ReturnDate,
                    srh.customer_id       AS CustomerId,
                    mc.customer_name      AS CustomerName,
                    srh.delivery_order_id AS DeliveryOrderId,
                    doh.do_number         AS DoNumber,
                    srh.sales_order_id    AS SalesOrderId,
                    so.so_number          AS SoNumber,
                    srh.notes             AS Notes,
                    srh.status            AS Status,
                    srh.created_at        AS CreatedAt
                FROM sales_return_header srh
                JOIN master_customer mc ON mc.customer_id = srh.customer_id
                LEFT JOIN delivery_order_header doh ON doh.id = srh.delivery_order_id
                LEFT JOIN sales_order so ON so.order_id = srh.sales_order_id
                WHERE srh.id = @Id";

            const string detailQuery = @"
                SELECT
                    srd.id           AS Id,
                    srd.return_id    AS ReturnId,
                    srd.product_id   AS ProductId,
                    mp.product_code  AS ProductCode,
                    mp.product_name  AS ProductName,
                    srd.warehouse_id AS WarehouseId,
                    mw.warehouse_name AS WarehouseName,
                    srd.qty          AS Qty,
                    srd.uom_id       AS UomId,
                    mu.uom_code      AS UomName,
                    srd.reason       AS Reason
                FROM sales_return_detail srd
                LEFT JOIN master_product mp ON mp.product_id = srd.product_id
                LEFT JOIN master_warehouse mw ON mw.warehouse_id = srd.warehouse_id
                LEFT JOIN master_uom mu ON mu.uom_id = srd.uom_id
                WHERE srd.return_id = @Id
                ORDER BY srd.id";

            using var connection = new SqlConnection(_connectionString);

            var header = await connection.QueryFirstOrDefaultAsync<SalesReturnHeaderDTO>(
                headerQuery, new { Id = id });

            if (header == null)
                return null;

            var details = await connection.QueryAsync<SalesReturnDetailDTO>(
                detailQuery, new { Id = id });

            return new SalesReturn { Header = header, Detail = details.ToList() };
        }

        public async Task<Dictionary<int, decimal>> GetReturnableQtyAsync(
            int deliveryOrderId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                SELECT
                    dod.product_id AS ProductId,
                    dod.qty_dikirim - ISNULL(returned.qty_returned, 0) AS Returnable
                FROM delivery_order_detail dod
                LEFT JOIN (
                    SELECT srd.product_id, SUM(srd.qty) AS qty_returned
                    FROM sales_return_detail srd
                    JOIN sales_return_header srh ON srh.id = srd.return_id
                    WHERE srh.delivery_order_id = @DeliveryOrderId
                    GROUP BY srd.product_id
                ) returned ON returned.product_id = dod.product_id
                WHERE dod.delivery_id = @DeliveryOrderId";

            var rows = await connection.QueryAsync<ReturnableRow>(
                query,
                new { DeliveryOrderId = deliveryOrderId },
                transaction);

            return rows.ToDictionary(r => r.ProductId, r => r.Returnable);
        }

        // Standalone read (own connection, no transaction) — used to preview
        // returnable qty in the DO picker before a return is actually submitted.
        public async Task<Dictionary<int, decimal>> GetReturnableQtyAsync(int deliveryOrderId)
        {
            const string query = @"
                SELECT
                    dod.product_id AS ProductId,
                    dod.qty_dikirim - ISNULL(returned.qty_returned, 0) AS Returnable
                FROM delivery_order_detail dod
                LEFT JOIN (
                    SELECT srd.product_id, SUM(srd.qty) AS qty_returned
                    FROM sales_return_detail srd
                    JOIN sales_return_header srh ON srh.id = srd.return_id
                    WHERE srh.delivery_order_id = @DeliveryOrderId
                    GROUP BY srd.product_id
                ) returned ON returned.product_id = dod.product_id
                WHERE dod.delivery_id = @DeliveryOrderId";

            using var connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<ReturnableRow>(
                query,
                new { DeliveryOrderId = deliveryOrderId });

            return rows.ToDictionary(r => r.ProductId, r => r.Returnable);
        }

        public async Task<int> InsertHeaderAsync(
            SalesReturnHeaderDTO header,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                INSERT INTO sales_return_header
                (
                    return_number,
                    return_date,
                    customer_id,
                    delivery_order_id,
                    sales_order_id,
                    notes,
                    status,
                    created_at
                )
                OUTPUT INSERTED.id
                VALUES
                (
                    @ReturnNumber,
                    @ReturnDate,
                    @CustomerId,
                    @DeliveryOrderId,
                    @SalesOrderId,
                    @Notes,
                    @Status,
                    DATEADD(HOUR, 7, GETUTCDATE())
                )";

            return await connection.ExecuteScalarAsync<int>(query, header, transaction);
        }

        public async Task InsertDetailAsync(
            SalesReturnDetailDTO detail,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                INSERT INTO sales_return_detail
                (
                    return_id,
                    product_id,
                    warehouse_id,
                    qty,
                    uom_id,
                    reason
                )
                VALUES
                (
                    @ReturnId,
                    @ProductId,
                    @WarehouseId,
                    @Qty,
                    @UomId,
                    @Reason
                )";

            await connection.ExecuteAsync(query, detail, transaction);
        }

        private sealed class ReturnableRow
        {
            public int ProductId { get; set; }
            public decimal Returnable { get; set; }
        }
    }
}
