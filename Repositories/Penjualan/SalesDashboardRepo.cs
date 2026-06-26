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
                    'Draft' AS Status
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

            using var connection = new SqlConnection(_connectionString);

            var dashboard = await connection.QueryFirstAsync<SalesDashboard>(summaryQuery);
            var recentOrders = await connection.QueryAsync<SalesDashboardOrderItem>(recentSalesOrderQuery);
            var recentInvoices = await connection.QueryAsync<SalesDashboardInvoiceItem>(recentInvoiceQuery);

            dashboard.RecentSalesOrders = recentOrders.ToList();
            dashboard.RecentInvoices = recentInvoices.ToList();

            return dashboard;
        }
    }
}
