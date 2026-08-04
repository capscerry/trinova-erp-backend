using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesInvoiceRepo
    {
        Task<List<SalesInvoiceHeader>> GetAll();
        Task<List<SalesInvoiceHeader>> GetOutstanding();
        Task<List<SalesInvoiceHeader>> GetByCustomerId(int customerId);
        Task<SalesInvoiceHeader?> GetHeaderById(int id);
        Task<List<SalesInvoiceDetail>> GetDetailByInvoiceId(int invoiceId);
        Task<int> InsertHeader(SalesInvoiceHeader header, SqlConnection connection, SqlTransaction transaction);
        Task UpdateHeader(SalesInvoiceHeader header, SqlConnection connection, SqlTransaction transaction);
        Task InsertDetail(SalesInvoiceDetail detail, SqlConnection connection, SqlTransaction transaction);
        Task DeleteDetailByInvoiceId(int invoiceId, SqlConnection connection, SqlTransaction transaction);
        Task DeleteInvoice(int id);
        Task ConfirmInvoice(int id);
    }

    public class SalesInvoiceRepo : ISalesInvoiceRepo
    {
        private readonly string _connectionString;

        public SalesInvoiceRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<List<SalesInvoiceHeader>> GetAll()
        {
            const string query = @"
                SELECT
                    si.id AS Id,
                    si.invoice_number AS InvoiceNumber,
                    si.customer_id AS CustomerId,
                    mc.customer_name AS CustomerName,
                    si.sales_order_id AS SalesOrderId,
                    so.so_number AS SalesOrderNumber,
                    si.delivery_order_id AS DeliveryOrderId,
                    doh.do_number AS DeliveryOrderNumber,
                    si.invoice_date AS InvoiceDate,
                    si.due_date AS DueDate,
                    si.status AS Status,
                    si.subtotal AS Subtotal,
                    si.discount_total AS DiscountTotal,
                    si.tax_total AS TaxTotal,
                    si.down_payment_amount AS DownPaymentAmount,
                    si.shipping_cost AS ShippingCost,
                    si.grand_total AS GrandTotal,
                    si.paid_amount AS PaidAmount,
                    si.remaining_amount AS RemainingAmount,
                    si.notes AS Notes,
                    si.proforma_stage AS ProformaStage,
                    si.created_by AS CreatedBy,
                    si.created_at AS CreatedAt,
                    si.updated_at AS UpdatedAt
                FROM sales_invoice si
                JOIN master_customer mc ON mc.customer_id = si.customer_id
                LEFT JOIN sales_order so ON so.order_id = si.sales_order_id
                LEFT JOIN delivery_order_header doh ON doh.id = si.delivery_order_id
                ORDER BY si.id DESC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<SalesInvoiceHeader>(query);
            return result.ToList();
        }

        public async Task<List<SalesInvoiceHeader>> GetOutstanding()
        {
            const string query = @"
                SELECT
                    si.id AS Id,
                    si.invoice_number AS InvoiceNumber,
                    si.customer_id AS CustomerId,
                    mc.customer_name AS CustomerName,
                    si.sales_order_id AS SalesOrderId,
                    so.so_number AS SalesOrderNumber,
                    si.delivery_order_id AS DeliveryOrderId,
                    doh.do_number AS DeliveryOrderNumber,
                    si.invoice_date AS InvoiceDate,
                    si.due_date AS DueDate,
                    si.status AS Status,
                    si.subtotal AS Subtotal,
                    si.discount_total AS DiscountTotal,
                    si.tax_total AS TaxTotal,
                    si.down_payment_amount AS DownPaymentAmount,
                    si.shipping_cost AS ShippingCost,
                    si.grand_total AS GrandTotal,
                    si.paid_amount AS PaidAmount,
                    si.remaining_amount AS RemainingAmount,
                    si.notes AS Notes
                FROM sales_invoice si
                JOIN master_customer mc ON mc.customer_id = si.customer_id
                LEFT JOIN sales_order so ON so.order_id = si.sales_order_id
                LEFT JOIN delivery_order_header doh ON doh.id = si.delivery_order_id
                WHERE si.remaining_amount > 0
                  AND si.status IN ('Issued', 'Partially Paid', 'Overdue', 'Belum Dibayar', 'Dibayar Sebagian', 'Draft')
                ORDER BY si.due_date ASC, si.id DESC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<SalesInvoiceHeader>(query);
            return result.ToList();
        }

        public async Task<List<SalesInvoiceHeader>> GetByCustomerId(int customerId)
        {
            const string query = @"
                SELECT
                    si.id AS Id,
                    si.invoice_number AS InvoiceNumber,
                    si.customer_id AS CustomerId,
                    mc.customer_name AS CustomerName,
                    si.sales_order_id AS SalesOrderId,
                    so.so_number AS SalesOrderNumber,
                    si.delivery_order_id AS DeliveryOrderId,
                    doh.do_number AS DeliveryOrderNumber,
                    si.invoice_date AS InvoiceDate,
                    si.due_date AS DueDate,
                    si.status AS Status,
                    si.subtotal AS Subtotal,
                    si.discount_total AS DiscountTotal,
                    si.tax_total AS TaxTotal,
                    si.down_payment_amount AS DownPaymentAmount,
                    si.shipping_cost AS ShippingCost,
                    si.grand_total AS GrandTotal,
                    si.paid_amount AS PaidAmount,
                    si.remaining_amount AS RemainingAmount,
                    si.notes AS Notes
                FROM sales_invoice si
                JOIN master_customer mc ON mc.customer_id = si.customer_id
                LEFT JOIN sales_order so ON so.order_id = si.sales_order_id
                LEFT JOIN delivery_order_header doh ON doh.id = si.delivery_order_id
                WHERE si.customer_id = @CustomerId
                ORDER BY si.id DESC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<SalesInvoiceHeader>(query, new { CustomerId = customerId });
            return result.ToList();
        }

        public async Task<SalesInvoiceHeader?> GetHeaderById(int id)
        {
            const string query = @"
                SELECT
                    si.id AS Id,
                    si.invoice_number AS InvoiceNumber,
                    si.customer_id AS CustomerId,
                    mc.customer_name AS CustomerName,
                    mc.email AS CustomerEmail,
                    si.sales_order_id AS SalesOrderId,
                    so.so_number AS SalesOrderNumber,
                    si.delivery_order_id AS DeliveryOrderId,
                    doh.do_number AS DeliveryOrderNumber,
                    si.invoice_date AS InvoiceDate,
                    si.due_date AS DueDate,
                    si.status AS Status,
                    si.subtotal AS Subtotal,
                    si.discount_total AS DiscountTotal,
                    si.tax_total AS TaxTotal,
                    si.down_payment_amount AS DownPaymentAmount,
                    si.shipping_cost AS ShippingCost,
                    si.grand_total AS GrandTotal,
                    si.paid_amount AS PaidAmount,
                    si.remaining_amount AS RemainingAmount,
                    si.notes AS Notes,
                    si.proforma_stage AS ProformaStage,
                    si.created_by AS CreatedBy,
                    si.created_at AS CreatedAt,
                    si.updated_at AS UpdatedAt
                FROM sales_invoice si
                JOIN master_customer mc ON mc.customer_id = si.customer_id
                LEFT JOIN sales_order so ON so.order_id = si.sales_order_id
                LEFT JOIN delivery_order_header doh ON doh.id = si.delivery_order_id
                WHERE si.id = @Id";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<SalesInvoiceHeader>(query, new { Id = id });
        }

        public async Task<List<SalesInvoiceDetail>> GetDetailByInvoiceId(int invoiceId)
        {
            const string query = @"
                SELECT
                    sid.id AS Id,
                    sid.sales_invoice_id AS SalesInvoiceId,
                    sid.product_id AS ProductId,
                    mp.product_code AS ProductCode,
                    mp.product_name AS ProductName,
                    sid.description AS Description,
                    sid.quantity AS Quantity,
                    sid.uom_id AS UomId,
                    mu.uom_code AS UomName,
                    sid.price AS Price,
                    sid.discount AS Discount,
                    sid.tax AS Tax,
                    sid.subtotal AS Subtotal,
                    sid.sales_order_item_id AS SalesOrderItemId,
                    sid.delivery_order_item_id AS DeliveryOrderItemId,
                    sid.created_at AS CreatedAt,
                    sid.updated_at AS UpdatedAt
                FROM sales_invoice_detail sid
                LEFT JOIN master_product mp ON mp.product_id = sid.product_id
                LEFT JOIN master_uom mu ON mu.uom_id = sid.uom_id
                WHERE sid.sales_invoice_id = @InvoiceId
                ORDER BY sid.id ASC";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<SalesInvoiceDetail>(query, new { InvoiceId = invoiceId });
            return result.ToList();
        }

        public async Task<int> InsertHeader(SalesInvoiceHeader header, SqlConnection connection, SqlTransaction transaction)
        {
            const string query = @"
                INSERT INTO sales_invoice
                (
                    invoice_number,
                    customer_id,
                    sales_order_id,
                    delivery_order_id,
                    invoice_date,
                    due_date,
                    status,
                    subtotal,
                    discount_total,
                    tax_total,
                    down_payment_amount,
                    shipping_cost,
                    grand_total,
                    paid_amount,
                    remaining_amount,
                    notes,
                    proforma_stage,
                    created_by,
                    created_at,
                    updated_at
                )
                OUTPUT INSERTED.id
                VALUES
                (
                    @InvoiceNumber,
                    @CustomerId,
                    @SalesOrderId,
                    @DeliveryOrderId,
                    @InvoiceDate,
                    @DueDate,
                    @Status,
                    @Subtotal,
                    @DiscountTotal,
                    @TaxTotal,
                    @DownPaymentAmount,
                    @ShippingCost,
                    @GrandTotal,
                    @PaidAmount,
                    @RemainingAmount,
                    @Notes,
                    @ProformaStage,
                    @CreatedBy,
                    DATEADD(HOUR, 7, GETUTCDATE()),
                    DATEADD(HOUR, 7, GETUTCDATE())
                )";

            return await connection.ExecuteScalarAsync<int>(query, header, transaction);
        }

        public async Task UpdateHeader(SalesInvoiceHeader header, SqlConnection connection, SqlTransaction transaction)
        {
            const string query = @"
                UPDATE sales_invoice
                SET
                    invoice_number = @InvoiceNumber,
                    customer_id = @CustomerId,
                    sales_order_id = @SalesOrderId,
                    delivery_order_id = @DeliveryOrderId,
                    invoice_date = @InvoiceDate,
                    due_date = @DueDate,
                    status = @Status,
                    subtotal = @Subtotal,
                    discount_total = @DiscountTotal,
                    tax_total = @TaxTotal,
                    down_payment_amount = @DownPaymentAmount,
                    shipping_cost = @ShippingCost,
                    grand_total = @GrandTotal,
                    paid_amount = @PaidAmount,
                    remaining_amount = @RemainingAmount,
                    notes = @Notes,
                    proforma_stage = @ProformaStage,
                    updated_at = DATEADD(HOUR, 7, GETUTCDATE())
                WHERE id = @Id";

            await connection.ExecuteAsync(query, header, transaction);
        }

        public async Task InsertDetail(SalesInvoiceDetail detail, SqlConnection connection, SqlTransaction transaction)
        {
            const string query = @"
                INSERT INTO sales_invoice_detail
                (
                    sales_invoice_id,
                    product_id,
                    description,
                    quantity,
                    uom_id,
                    price,
                    discount,
                    tax,
                    subtotal,
                    sales_order_item_id,
                    delivery_order_item_id,
                    warehouse_id,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @SalesInvoiceId,
                    @ProductId,
                    @Description,
                    @Quantity,
                    @UomId,
                    @Price,
                    @Discount,
                    @Tax,
                    @Subtotal,
                    @SalesOrderItemId,
                    @DeliveryOrderItemId,
                    @WarehouseId,
                    DATEADD(HOUR, 7, GETUTCDATE()),
                    DATEADD(HOUR, 7, GETUTCDATE())
                )";

            await connection.ExecuteAsync(query, detail, transaction);
        }

        public async Task DeleteDetailByInvoiceId(int invoiceId, SqlConnection connection, SqlTransaction transaction)
        {
            const string query = "DELETE FROM sales_invoice_detail WHERE sales_invoice_id = @InvoiceId";
            await connection.ExecuteAsync(query, new { InvoiceId = invoiceId }, transaction);
        }

        public async Task DeleteInvoice(int id)
        {
            const string detailQuery = "DELETE FROM sales_invoice_detail WHERE sales_invoice_id = @Id";
            const string headerQuery = "DELETE FROM sales_invoice WHERE id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                await connection.ExecuteAsync(detailQuery, new { Id = id }, transaction);
                await connection.ExecuteAsync(headerQuery, new { Id = id }, transaction);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ConfirmInvoice(int id)
        {
            const string query = @"
                UPDATE sales_invoice
                SET
                    status = CASE
                        WHEN remaining_amount <= 0 THEN 'Paid'
                        WHEN paid_amount > 0 THEN 'Partially Paid'
                        ELSE 'Issued'
                    END,
                    updated_at = DATEADD(HOUR, 7, GETUTCDATE())
                WHERE id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query, new { Id = id });
        }
    }
}
