using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesDashboardRepo
    {
        Task<SalesDashboard> GetDashboard();
    }

    public class SalesDashboardRepo : ISalesDashboardRepo
    {
        private readonly string _connectionString;

        public SalesDashboardRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<SalesDashboard> GetDashboard()
        {
            const string summaryQuery = @"
                SELECT
                    ISNULL((SELECT SUM(ISNULL(subtotal, 0)) FROM sales_order), 0) AS TotalSalesOrder,
                    ISNULL((SELECT COUNT(1) FROM sales_order), 0) AS SalesOrderCount,
                    ISNULL((SELECT SUM(ISNULL(grand_total, 0)) FROM sales_invoice), 0) AS TotalInvoice,
                    ISNULL((SELECT COUNT(1) FROM sales_invoice), 0) AS InvoiceCount,
                    ISNULL((SELECT SUM(ISNULL(remaining_amount, 0)) FROM sales_invoice), 0) AS OutstandingInvoice,
                    ISNULL((SELECT SUM(ISNULL(nilai_pembayaran, 0)) FROM sales_receipt), 0) AS TotalReceipt,
                    ISNULL((SELECT COUNT(1) FROM sales_receipt), 0) AS ReceiptCount,
                    ISNULL((SELECT COUNT(1) FROM delivery_order_header), 0) AS DeliveryCount,
                    ISNULL((SELECT COUNT(1) FROM master_customer), 0) AS CustomerCount;";

            const string recentSalesOrderQuery = @"
                SELECT TOP 5
                    so.order_id AS Id,
                    so.so_number AS Number,
                    mc.customer_name AS CustomerName,
                    so.so_date AS Date,
                    ISNULL(so.subtotal, 0) AS Total,
                    ISNULL(so.status, 'Draft') AS Status
                FROM sales_order so
                LEFT JOIN master_customer mc ON mc.customer_id = so.customer_id
                ORDER BY so.order_id DESC;";

            const string recentInvoiceQuery = @"
                SELECT TOP 5
                    si.id AS Id,
                    si.invoice_number AS Number,
                    mc.customer_name AS CustomerName,
                    si.invoice_date AS Date,
                    ISNULL(si.grand_total, 0) AS GrandTotal,
                    ISNULL(si.remaining_amount, 0) AS RemainingAmount,
                    ISNULL(si.status, 'Draft') AS Status
                FROM sales_invoice si
                LEFT JOIN master_customer mc ON mc.customer_id = si.customer_id
                ORDER BY si.id DESC;";

            const string recentActivityQuery = @"
                SELECT TOP 8
                    Id,
                    Module,
                    ActivityType,
                    Title,
                    Description,
                    RefTable,
                    RefId,
                    RefNumber,
                    UserName,
                    CreatedAt
                FROM ActivityLogs
                WHERE Module = 'sales'
                ORDER BY CreatedAt DESC, Id DESC;";

            const string upcomingActivityQuery = @"
                SELECT TOP 8 *
                FROM
                (
                    SELECT
                        'invoice_due' AS ActivityType,
                        CONCAT('Invoice ', si.invoice_number, ' due soon') AS Title,
                        CONCAT('Remaining amount Rp ', FORMAT(ISNULL(si.remaining_amount, 0), 'N0', 'id-ID')) AS Description,
                        'sales_invoice' AS RefTable,
                        CAST(si.id AS BIGINT) AS RefId,
                        si.invoice_number AS RefNumber,
                        CAST(si.due_date AS DATETIME) AS ActivityDate,
                        CASE
                            WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(si.due_date AS DATE)) <= 0 THEN 'danger'
                            WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(si.due_date AS DATE)) <= 3 THEN 'warning'
                            ELSE 'normal'
                        END AS Priority
                    FROM sales_invoice si
                    WHERE si.due_date IS NOT NULL
                      AND ISNULL(si.remaining_amount, 0) > 0
                      AND ISNULL(si.status, 'Issued') IN ('Issued', 'Partially Paid', 'Overdue', 'Terbit', 'Dibayar Sebagian')
                      AND CAST(si.due_date AS DATE) >= CAST(GETDATE() AS DATE)

                    UNION ALL

                    SELECT
                        'sales_order_delivery' AS ActivityType,
                        CONCAT('Sales Order ', so.so_number, ' scheduled for delivery') AS Title,
                        mc.customer_name AS Description,
                        'sales_order' AS RefTable,
                        CAST(so.order_id AS BIGINT) AS RefId,
                        so.so_number AS RefNumber,
                        CAST(so.tanggal_kirim AS DATETIME) AS ActivityDate,
                        CASE
                            WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(so.tanggal_kirim AS DATE)) <= 0 THEN 'danger'
                            WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(so.tanggal_kirim AS DATE)) <= 3 THEN 'warning'
                            ELSE 'normal'
                        END AS Priority
                    FROM sales_order so
                    LEFT JOIN master_customer mc ON mc.customer_id = so.customer_id
                    WHERE so.tanggal_kirim IS NOT NULL
                      AND ISNULL(so.status, 'Draft') IN ('Draft', 'Approved', 'Confirmed', 'Processing')
                      AND CAST(so.tanggal_kirim AS DATE) >= CAST(GETDATE() AS DATE)

                    UNION ALL

                    SELECT
                        'delivery_order_follow_up' AS ActivityType,
                        CONCAT('Delivery Order ', doh.do_number, ' needs follow up') AS Title,
                        mc.customer_name AS Description,
                        'delivery_order_header' AS RefTable,
                        CAST(doh.id AS BIGINT) AS RefId,
                        doh.do_number AS RefNumber,
                        CAST(doh.do_date AS DATETIME) AS ActivityDate,
                        CASE
                            WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(doh.do_date AS DATE)) <= 0 THEN 'danger'
                            WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(doh.do_date AS DATE)) <= 3 THEN 'warning'
                            ELSE 'normal'
                        END AS Priority
                    FROM delivery_order_header doh
                    LEFT JOIN master_customer mc ON mc.customer_id = doh.customer_id
                    WHERE doh.do_date IS NOT NULL
                      AND ISNULL(doh.status, 'Draft') IN ('Draft', 'Approved', 'Shipped', 'Received')
                      AND CAST(doh.do_date AS DATE) >= CAST(GETDATE() AS DATE)
                ) upcoming
                ORDER BY ActivityDate ASC;";

            using var connection = new SqlConnection(_connectionString);

            var dashboard = await connection.QueryFirstAsync<SalesDashboard>(summaryQuery);
            var recentOrders = await connection.QueryAsync<SalesDashboardOrderItem>(recentSalesOrderQuery);
            var recentInvoices = await connection.QueryAsync<SalesDashboardInvoiceItem>(recentInvoiceQuery);
            var recentActivities = await connection.QueryAsync<SalesDashboardActivityItem>(recentActivityQuery);
            var upcomingActivities = await connection.QueryAsync<SalesDashboardUpcomingActivityItem>(upcomingActivityQuery);

            dashboard.RecentSalesOrders = recentOrders.ToList();
            dashboard.RecentInvoices = recentInvoices.ToList();
            dashboard.RecentActivities = recentActivities.ToList();
            dashboard.UpcomingActivities = upcomingActivities.ToList();

            return dashboard;
        }
    }
}
