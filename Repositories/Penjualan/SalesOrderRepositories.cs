using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesOrderRepositories
    {
        Task<SalesOrderHeader> UpsertSalesOrderHeader(
            SalesOrderHeader header,
            IDbConnection connection,
            IDbTransaction tx
        );

        Task<List<SalesOrderHeader>> GetSalesOrderByCustomerId(int customerId);

        Task<SalesOrderDetail> UpsertSalesOrderDetail(
            SalesOrderDetail detail,
            IDbConnection connection,
            IDbTransaction tx
        );

        Task<List<SalesOrderHeader>> GetAllSalesOrder();

        Task<SalesOrderDetailDTO> GetSalesOrderDetail(int orderId);

        Task<Dictionary<int, decimal>> GetShippedQuantitiesAsync(
            int orderId,
            IDbConnection connection,
            IDbTransaction tx
        );

        Task SetSalesOrderCancelledAsync(
            int orderId,
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

        public async Task<SalesOrderHeader> UpsertSalesOrderHeader(
    SalesOrderHeader header,
    IDbConnection connection,
    IDbTransaction tx
)
        {
            try
            {
                string query = @"
            IF EXISTS (
                SELECT 1
                FROM sales_order
                WHERE order_id = @OrderId
            )
            BEGIN
                UPDATE sales_order
                SET
                    so_number = @SoNumber,
                    tanggal_kirim = @TanggalKirim,
                    so_date = @SoDate,
                    po_number = @PoNumber,
                    subtotal = @SubTotal,
                    customer_id = @CustomerId,
                    is_taxable = @IsTaxAble,
                    is_tax_included = @IsTaxIncluded,
                    address = @Address,
                    notes = @Notes,
                    discount_total = @DiscountTotal,
                    quotation_id = @QuotationId,
                    tax_total = @TaxTotal,
                    is_indent = @IsIndent
                WHERE order_id = @OrderId;
            END
            ELSE
            BEGIN
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
                    notes,
                    discount_total,
                    quotation_id,
                    tax_total,
                    is_indent,
                    status
                )
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
                    @Notes,
                    @DiscountTotal,
                    @QuotationId,
                    @TaxTotal,
                    @IsIndent,
                    'Draft'
                );

                SET @OrderId = CAST(SCOPE_IDENTITY() AS INT);
            END

            SELECT
                order_id AS OrderId,
                customer_id AS CustomerId,
                so_number AS SoNumber,
                tanggal_kirim AS TanggalKirim,
                so_date AS SoDate,
                po_number AS PoNumber,
                subtotal AS SubTotal,
                is_taxable AS IsTaxAble,
                is_tax_included AS IsTaxIncluded,
                address AS Address,
                notes AS Notes,
                discount_total AS DiscountTotal,
                quotation_id   AS QuotationId,
                tax_total AS TaxTotal,
                is_indent AS IsIndent,
                status AS Status
            FROM sales_order
            WHERE order_id = @OrderId;
        ";

                var result = await connection.QuerySingleAsync<SalesOrderHeader>(
                    query,
                    header,
                    tx
                );

                if (header.QuotationId.HasValue && header.QuotationId.Value > 0)
                {
                    const string updateQuotationStatusQuery = @"
                        UPDATE sales_quotation
                        SET status = 'Processed'
                        WHERE quotation_id = @QuotationId;";

                    await connection.ExecuteAsync(
                        updateQuotationStatusQuery,
                        new { header.QuotationId },
                        tx
                    );
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal melakukan upsert sales order header: {ex.Message}", ex);
            }
        }

        public async Task<SalesOrderDetail> UpsertSalesOrderDetail(
    SalesOrderDetail detail,
    IDbConnection connection,
    IDbTransaction tx
)
        {
            try
            {
                string query = @"
            IF EXISTS (
                SELECT 1
                FROM sales_order_detail
                WHERE order_id = @OrderId
                  AND product_id = @ProductId
            )
            BEGIN
                UPDATE sales_order_detail
                SET
                    product_code = @ProductCode,
                    product_name = @ProductName,
                    product_qty = @ProductQty,
                    product_price = @ProductPrice,
                    discount_percent = @DiscountPercent,
                    total_price = @TotalPrice,
                    warehouse_id = @WareHouseId,
                    uom_id = @UomId
                WHERE order_id = @OrderId
                  AND product_id = @ProductId;
            END
            ELSE
            BEGIN
                INSERT INTO sales_order_detail
                (
                    order_id,
                    product_id,
                    product_code,
                    product_name,
                    product_qty,
                    product_price,
                    discount_percent,
                    total_price,
                    warehouse_id,
                    uom_id
                )
                VALUES
                (
                    @OrderId,
                    @ProductId,
                    @ProductCode,
                    @ProductName,
                    @ProductQty,
                    @ProductPrice,
                    @DiscountPercent,
                    @TotalPrice,
                    @WarehouseId,
                    @UomId
                );
            END

            SELECT
                order_id AS OrderId,
                product_id AS ProductId,
                product_code AS ProductCode,
                product_name AS ProductName,
                product_qty AS ProductQty,
                product_price AS ProductPrice,
                discount_percent AS DiscountPercent,
                total_price AS TotalPrice,
                warehouse_id AS WarehouseId,
                uom_id AS UomId
            FROM sales_order_detail
            WHERE order_id = @OrderId
              AND product_id = @ProductId;
        ";

                return await connection.QuerySingleAsync<SalesOrderDetail>(
                    query,
                    detail,
                    tx
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal melakukan upsert sales order detail: {ex.Message}", ex);
            }
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
                     so.notes           AS Notes,
                     so.is_indent       AS IsIndent,
                     ISNULL(so.status, 'Draft') AS Status
                 FROM sales_order so JOIN master_customer mc  ON so.customer_id  = mc.customer_id
                 ORDER BY order_id DESC;
            ";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<SalesOrderHeader>(query);

            return result.ToList();
        }

        public async Task<List<SalesOrderHeader>> GetSalesOrderByCustomerId(int customerId)
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
                     so.notes           AS Notes,
                     so.is_indent       AS IsIndent,
                     ISNULL(so.status, 'Draft') AS Status
                 FROM sales_order so JOIN master_customer mc  ON so.customer_id  = mc.customer_id
                 WHERE mc.customer_id = @CustomerId
                 ORDER BY order_id DESC";

            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryAsync<SalesOrderHeader>(
               query,
               new
               {
                   CustomerId = customerId
               });

            return result.ToList();
        }

        public async Task<SalesOrderDetailDTO?> GetSalesOrderDetail(int orderId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);

                string headerQuery = @"
            SELECT 
                so.so_number AS SoNumber,
                mc.customer_name AS CustomerName,
                mc.customer_id  AS CustomerId,
                so.order_id AS OrderId,
                so.so_date AS SoDate,
                so.tanggal_kirim AS TanggalKirim,
                so.po_number AS PoNumber,
                so.address AS Address,
                so.subtotal AS Total,
                so.discount_total AS DiscountTotal,
                so.tax_total AS TaxTotal,
                so.is_taxable AS IsTaxAble,
                so.notes AS Keterangan,
                ISNULL(so.status, 'Draft') AS Status,
                so.is_indent AS IsIndent,
                so.quotation_id AS QuotationId,
                sq.quotation_number AS QuotationNumber
            FROM sales_order so
            JOIN master_customer mc
                ON mc.customer_id = so.customer_id
            LEFT JOIN sales_quotation sq
                ON sq.quotation_id = so.quotation_id
            WHERE so.order_id = @OrderId
        ";

                string detailQuery = @"
            SELECT 
                mp.product_name AS ProductName,
                mp.product_id   AS ProductId,
                sod.product_qty AS ProductQty,
                sod.product_price AS ProductPrice,
                sod.discount_percent AS ProductDiscount,
                sod.total_price AS TotalPrice,
                sod.warehouse_id AS WareHouseId,
                mw.warehouse_name AS WarehouseName,
                sod.uom_id AS UomId,
                mu.uom_code AS UomCode
            FROM sales_order_detail sod
            JOIN master_product mp
                ON mp.product_id = sod.product_id
            LEFT JOIN master_warehouse mw
	            ON sod.warehouse_id  = mw.warehouse_id
            JOIN master_uom mu
                ON sod.uom_id = mu.uom_id
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
            catch (Exception ex)
            {
                throw new Exception(
                    $"Terjadi kesalahan saat mengambil Sales Order Detail. OrderId: {orderId}. Error: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<Dictionary<int, decimal>> GetShippedQuantitiesAsync(
            int orderId,
            IDbConnection connection,
            IDbTransaction tx
        )
        {
            const string query = @"
                SELECT dod.product_id AS ProductId, SUM(dod.qty_dikirim) AS Shipped
                FROM delivery_order_detail dod
                JOIN delivery_order_header doh ON doh.id = dod.delivery_id
                WHERE doh.so_id = @OrderId
                GROUP BY dod.product_id";

            var rows = await connection.QueryAsync<ShippedQuantityRow>(
                query,
                new { OrderId = orderId },
                tx
            );

            return rows.ToDictionary(r => r.ProductId, r => r.Shipped);
        }

        private sealed class ShippedQuantityRow
        {
            public int ProductId { get; set; }
            public decimal Shipped { get; set; }
        }

        public async Task SetSalesOrderCancelledAsync(
            int orderId,
            IDbConnection connection,
            IDbTransaction tx
        )
        {
            const string query = @"
                UPDATE sales_order
                SET status = 'Cancelled'
                WHERE order_id = @OrderId;";

            await connection.ExecuteAsync(query, new { OrderId = orderId }, tx);
        }
    }
}
