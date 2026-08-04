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
        Task<List<DeliveryOrderDetailDTO>> GetAllDoDetails();
        Task<int?> GetSalesOrderLineWarehouseAsync(
            int soId,
            int productId,
            SqlConnection connection,
            SqlTransaction transaction);

        Task<bool> IsSalesOrderFullyInvoicedAndPaidAsync(
            int soId,
            SqlConnection connection,
            SqlTransaction transaction);

        Task<(string? Status, int? SoId)> GetStatusAndSoIdAsync(
            int deliveryOrderId,
            SqlConnection connection,
            SqlTransaction transaction);

        Task UpdateDeliveryOrderStatusAsync(
            int deliveryOrderId,
            string status,
            SqlConnection connection,
            SqlTransaction transaction);

        Task UpdateSalesOrderStatusAsync(
            int soId,
            string status,
            SqlConnection connection,
            SqlTransaction transaction);

        /// <summary>
        /// Aggregate qty_dipesan/qty_dikirim across every non-Cancelled Delivery
        /// Order tied to this SO. Used to decide whether marking one DO as
        /// received should finish the SO ("Completed") or leave it as
        /// "Partially Fulfilled" -- an SO can have more than one DO (partial
        /// shipments), so a single DO's own qty isn't enough to tell.
        /// </summary>
        Task<(int TotalOrdered, int TotalShipped)> GetSalesOrderFulfillmentAsync(
            int soId,
            SqlConnection connection,
            SqlTransaction transaction);

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
                    dod.warehouse_id AS WarehouseId,
                    mw.warehouse_name AS WarehouseName,
                    sod.uom_id AS UomId,
                    mu.uom_code AS UomName
                FROM delivery_order_detail dod
                LEFT JOIN master_product mp ON mp.product_id = dod.product_id
                LEFT JOIN delivery_order_header doh ON doh.id = dod.delivery_id
                LEFT JOIN sales_order_detail sod
                    ON sod.order_id = doh.so_id
                   AND sod.product_id = dod.product_id
                LEFT JOIN master_uom mu ON mu.uom_id = sod.uom_id
                LEFT JOIN master_warehouse mw ON mw.warehouse_id = dod.warehouse_id
                WHERE dod.delivery_id = @DeliveryOrderId
                ORDER BY dod.product_id ASC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<DeliveryOrderDetailDTO>(
                query,
                new { DeliveryOrderId = deliveryOrderId });

            return result.ToList();
        }

        // All delivery order detail lines across every DO -- used by the Sales
        // dashboard to compute a true qty-based Fulfillment Rate (qty shipped
        // vs qty ordered), instead of relying on the SO's header status alone.
        public async Task<List<DeliveryOrderDetailDTO>> GetAllDoDetails()
        {
            const string query = @"
                SELECT
                    dod.delivery_id AS DoId,
                    dod.product_id AS ProductId,
                    mp.product_code AS ProductCode,
                    mp.product_name AS ProductName,
                    dod.qty_dikirim AS QtyDikirim,
                    dod.qty_dipesan AS QtyDipesan,
                    dod.warehouse_id AS WarehouseId,
                    mw.warehouse_name AS WarehouseName,
                    sod.uom_id AS UomId,
                    mu.uom_code AS UomName
                FROM delivery_order_detail dod
                LEFT JOIN master_product mp ON mp.product_id = dod.product_id
                LEFT JOIN delivery_order_header doh ON doh.id = dod.delivery_id
                LEFT JOIN sales_order_detail sod
                    ON sod.order_id = doh.so_id
                   AND sod.product_id = dod.product_id
                LEFT JOIN master_uom mu ON mu.uom_id = sod.uom_id
                LEFT JOIN master_warehouse mw ON mw.warehouse_id = dod.warehouse_id
                ORDER BY dod.delivery_id ASC, dod.product_id ASC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<DeliveryOrderDetailDTO>(query);
            return result.ToList();
        }

        public async Task<int?> GetSalesOrderLineWarehouseAsync(
            int soId,
            int productId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                SELECT warehouse_id
                FROM sales_order_detail
                WHERE order_id = @SoId AND product_id = @ProductId";

            return await connection.QueryFirstOrDefaultAsync<int?>(
                query,
                new { SoId = soId, ProductId = productId },
                transaction);
        }


        // Cek apakah semua Sales Invoice non-Cancelled milik SO ini sudah
        // menutupi seluruh subtotal SO DAN semuanya sudah lunas (remaining
        // amount <= 0). Dipakai sebagai gate sebelum Delivery Order boleh
        // dibuat -- flow baru: DO cuma boleh dibuat setelah invoice terkait
        // (baik 1 invoice reguler maupun 2 invoice proforma DP+pelunasan
        // untuk barang indent) lunas 100%.
        public async Task<bool> IsSalesOrderFullyInvoicedAndPaidAsync(
            int soId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            // so.subtotal adalah grand total SO (SUDAH termasuk PPN -- lihat
            // komentar mapFormToApiPayload di frontend SalesOrderType.ts),
            // sedangkan si.subtotal per invoice adalah subtotal KOTOR sebelum
            // diskon & PPN. Membandingkan keduanya apa adanya membuat SO ber-
            // PPN TIDAK PERNAH dianggap lunas (selalu kurang sebesar nilai
            // pajaknya) walau seluruh invoice sudah lunas. Rekonstruksi basis
            // yang sepadan: (subtotal - discount_total + tax_total) per invoice.
            const string query = @"
                SELECT
                    so.subtotal AS SoSubtotal,
                    ISNULL(SUM(CASE WHEN si.status <> 'Cancelled' THEN (si.subtotal - si.discount_total + si.tax_total) ELSE 0 END), 0) AS InvoicedSubtotal,
                    SUM(CASE WHEN si.status <> 'Cancelled' AND ISNULL(si.remaining_amount, 0) > 0 THEN 1 ELSE 0 END) AS UnpaidInvoiceCount
                FROM sales_order so
                LEFT JOIN sales_invoice si ON si.sales_order_id = so.order_id
                WHERE so.order_id = @SoId
                GROUP BY so.subtotal";

            var row = await connection.QueryFirstOrDefaultAsync(
                query,
                new { SoId = soId },
                transaction);

            if (row == null)
                return false;

            decimal soSubtotal = row.SoSubtotal;
            decimal invoicedSubtotal = row.InvoicedSubtotal;
            int unpaidInvoiceCount = (int)row.UnpaidInvoiceCount;

            return invoicedSubtotal >= soSubtotal && unpaidInvoiceCount == 0 && invoicedSubtotal > 0;
        }

        public async Task<(string? Status, int? SoId)> GetStatusAndSoIdAsync(
            int deliveryOrderId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                SELECT ISNULL(status, 'Draft') AS Status, so_id AS SoId
                FROM delivery_order_header
                WHERE id = @Id";

            var row = await connection.QueryFirstOrDefaultAsync(
                query,
                new { Id = deliveryOrderId },
                transaction);

            if (row == null)
                return (null, null);

            return ((string)row.Status, (int?)row.SoId);
        }

        public async Task UpdateDeliveryOrderStatusAsync(
            int deliveryOrderId,
            string status,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                UPDATE delivery_order_header
                SET status = @Status
                WHERE id = @Id";

            await connection.ExecuteAsync(
                query,
                new { Id = deliveryOrderId, Status = status },
                transaction);
        }

        public async Task UpdateSalesOrderStatusAsync(
            int soId,
            string status,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                UPDATE sales_order
                SET status = @Status
                WHERE order_id = @SoId";

            await connection.ExecuteAsync(
                query,
                new { SoId = soId, Status = status },
                transaction);
        }

        public async Task<(int TotalOrdered, int TotalShipped)> GetSalesOrderFulfillmentAsync(
            int soId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                SELECT
                    ISNULL(SUM(dod.qty_dipesan), 0) AS TotalOrdered,
                    ISNULL(SUM(dod.qty_dikirim), 0) AS TotalShipped
                FROM delivery_order_detail dod
                JOIN delivery_order_header doh ON doh.id = dod.delivery_id
                WHERE doh.so_id = @SoId
                  AND doh.status <> 'Cancelled'";

            var row = await connection.QuerySingleAsync(
                query,
                new { SoId = soId },
                transaction);

            return ((int)row.TotalOrdered, (int)row.TotalShipped);
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
            so_id,
            status
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
            @SoId,
            'In Delivery'
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
            qty_dipesan,
            warehouse_id
        )
        VALUES
        (
            @DoId,
            @ProductId,
            @QtyDikirim,
            @QtyDipesan,
            @WarehouseId
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
