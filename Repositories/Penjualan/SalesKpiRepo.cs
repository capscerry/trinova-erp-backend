using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Linq;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface ISalesKpiRepo
    {
        Task<SalesKpiResult> GetSalesKpiAsync(SalesKpiQuery query);
    }

    /// <summary>
    /// Computes the Sales executive-dashboard KPI cards, charts, funnel and
    /// leaderboard entirely in SQL, instead of the dashboard fetching every
    /// raw sales_order/sales_invoice/delivery_order/... row over HTTP and
    /// aggregating it in the browser. Filtering (date range, customer,
    /// category, status) all happens server-side so what the dashboard shows
    /// is always exactly what a direct SQL query against these tables would
    /// show -- no client-side date-parsing/filter logic left to silently
    /// disagree with the real data.
    /// </summary>
    public class SalesKpiRepo : ISalesKpiRepo
    {
        private readonly string _connectionString;

        public SalesKpiRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<SalesKpiResult> GetSalesKpiAsync(SalesKpiQuery q)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = new SalesKpiResult();

            // ── Sales Order count (current vs prev period) ──────────────────
            // Mirrors old frontend `filterSO`: only status + category filter,
            // NOT customerId (Sales Order doesn't carry a category filter join
            // directly -- go through master_customer/master_customer_category).
            const string soCountQuery = @"
                SELECT COUNT(*)
                FROM sales_order so
                JOIN master_customer c ON c.customer_id = so.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE so.so_date BETWEEN @From AND @To
                  AND (@Status IS NULL OR so.status = @Status)
                  AND (@Category IS NULL OR cat.category_name = @Category)";

            result.Kpis.TotalSalesOrder = await connection.ExecuteScalarAsync<int>(soCountQuery,
                new { q.From, q.To, q.Status, q.Category });
            result.Kpis.TotalSalesOrderPrevMonth = await connection.ExecuteScalarAsync<int>(soCountQuery,
                new { From = q.PrevFrom, To = q.PrevTo, q.Status, q.Category });

            // ── Total Revenue from invoices (current vs prev period) ────────
            const string revenueQuery = @"
                SELECT ISNULL(SUM(si.grand_total), 0)
                FROM sales_invoice si
                JOIN master_customer c ON c.customer_id = si.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE si.invoice_date BETWEEN @From AND @To
                  AND (@CustomerId IS NULL OR si.customer_id = @CustomerId)
                  AND (@Category IS NULL OR cat.category_name = @Category)";

            result.Kpis.TotalRevenue = await connection.ExecuteScalarAsync<decimal>(revenueQuery,
                new { q.From, q.To, q.CustomerId, q.Category });
            result.Kpis.TotalRevenuePrevMonth = await connection.ExecuteScalarAsync<decimal>(revenueQuery,
                new { From = q.PrevFrom, To = q.PrevTo, q.CustomerId, q.Category });

            // ── Outstanding Receivable (current snapshot, not date-ranged) ──
            const string outstandingQuery = @"
                SELECT ISNULL(SUM(si.remaining_amount), 0)
                FROM sales_invoice si
                JOIN master_customer c ON c.customer_id = si.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE si.status NOT IN ('Paid', 'Cancelled')
                  AND (@CustomerId IS NULL OR si.customer_id = @CustomerId)
                  AND (@Category IS NULL OR cat.category_name = @Category)";

            result.Kpis.OutstandingReceivable = await connection.ExecuteScalarAsync<decimal>(outstandingQuery,
                new { q.CustomerId, q.Category });

            // ── Fulfillment Rate: qty shipped / qty ordered, DOs in range ───
            const string fulfillmentQuery = @"
                SELECT
                    ISNULL(SUM(dod.qty_dipesan), 0) AS TotalOrdered,
                    ISNULL(SUM(dod.qty_dikirim), 0) AS TotalShipped
                FROM delivery_order_detail dod
                JOIN delivery_order_header doh ON doh.id = dod.delivery_id
                WHERE doh.do_date BETWEEN @From AND @To
                  AND doh.status <> 'Cancelled'";

            var fulfillment = await connection.QuerySingleAsync(fulfillmentQuery, new { q.From, q.To });
            int totalOrdered = (int)fulfillment.TotalOrdered;
            int totalShipped = (int)fulfillment.TotalShipped;
            result.Kpis.FulfillmentRate = totalOrdered > 0
                ? Math.Min((double)totalShipped / totalOrdered * 100.0, 100.0)
                : 0;

            // ── Sales Return count (current vs prev period) ─────────────────
            const string returnCountQuery = @"
                SELECT COUNT(*) FROM sales_return_header WHERE return_date BETWEEN @From AND @To";

            result.Kpis.SalesReturnCount = await connection.ExecuteScalarAsync<int>(returnCountQuery,
                new { q.From, q.To });
            result.Kpis.SalesReturnPrevMonth = await connection.ExecuteScalarAsync<int>(returnCountQuery,
                new { From = q.PrevFrom, To = q.PrevTo });

            // ── Charts: Revenue Trend (monthly) ──────────────────────────────
            const string revenueTrendQuery = @"
                SELECT
                    CAST(YEAR(si.invoice_date) AS VARCHAR) + '-' + RIGHT('0' + CAST(MONTH(si.invoice_date) AS VARCHAR), 2) AS MonthKey,
                    SUM(si.grand_total) AS Value
                FROM sales_invoice si
                JOIN master_customer c ON c.customer_id = si.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE si.invoice_date BETWEEN @From AND @To
                  AND (@CustomerId IS NULL OR si.customer_id = @CustomerId)
                  AND (@Category IS NULL OR cat.category_name = @Category)
                GROUP BY YEAR(si.invoice_date), MONTH(si.invoice_date)";

            var revenueByMonth = (await connection.QueryAsync(revenueTrendQuery,
                new { q.From, q.To, q.CustomerId, q.Category }))
                .ToDictionary(r => (string)r.MonthKey, r => (decimal)r.Value);
            result.Charts.RevenueTrend = BuildMonthlyPoints(revenueByMonth, q.From, q.To);

            // ── Charts: Sales Volume Trend (monthly) ─────────────────────────
            const string volumeTrendQuery = @"
                SELECT
                    CAST(YEAR(so.so_date) AS VARCHAR) + '-' + RIGHT('0' + CAST(MONTH(so.so_date) AS VARCHAR), 2) AS MonthKey,
                    COUNT(*) AS Value
                FROM sales_order so
                JOIN master_customer c ON c.customer_id = so.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE so.so_date BETWEEN @From AND @To
                  AND (@Status IS NULL OR so.status = @Status)
                  AND (@Category IS NULL OR cat.category_name = @Category)
                GROUP BY YEAR(so.so_date), MONTH(so.so_date)";

            var volumeByMonth = (await connection.QueryAsync(volumeTrendQuery,
                new { q.From, q.To, q.Status, q.Category }))
                .ToDictionary(r => (string)r.MonthKey, r => (decimal)(int)r.Value);
            result.Charts.SalesVolumeTrend = BuildMonthlyPoints(volumeByMonth, q.From, q.To);

            // ── Charts: SO Status Distribution ───────────────────────────────
            const string statusDistQuery = @"
                SELECT so.status AS Status, COUNT(*) AS Count
                FROM sales_order so
                JOIN master_customer c ON c.customer_id = so.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE so.so_date BETWEEN @From AND @To
                  AND (@Status IS NULL OR so.status = @Status)
                  AND (@Category IS NULL OR cat.category_name = @Category)
                GROUP BY so.status";

            result.Charts.SoStatusDistribution = (await connection.QueryAsync<SOStatusCountDTO>(statusDistQuery,
                new { q.From, q.To, q.Status, q.Category })).ToList();

            // ── Charts: Top 10 Customers by Revenue + Leaderboard ────────────
            const string topCustomersQuery = @"
                SELECT TOP 10
                    si.customer_id AS CustomerId,
                    c.customer_name AS CustomerName,
                    SUM(si.grand_total) AS Total,
                    COUNT(*) AS InvoiceCount,
                    ISNULL(cat.category_name, 'Others') AS Category
                FROM sales_invoice si
                JOIN master_customer c ON c.customer_id = si.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE si.invoice_date BETWEEN @From AND @To
                  AND (@CustomerId IS NULL OR si.customer_id = @CustomerId)
                  AND (@Category IS NULL OR cat.category_name = @Category)
                GROUP BY si.customer_id, c.customer_name, cat.category_name
                ORDER BY SUM(si.grand_total) DESC";

            var topCustomerRows = (await connection.QueryAsync(topCustomersQuery,
                new { q.From, q.To, q.CustomerId, q.Category })).ToList();

            result.Charts.TopCustomersByRevenue = topCustomerRows
                .Select(r => new CustomerRevenueDTO
                {
                    CustomerId = (int)r.CustomerId,
                    CustomerName = (string)r.CustomerName,
                    Total = (decimal)r.Total
                }).ToList();

            result.Leaderboard = topCustomerRows
                .Select((r, i) => new CustomerLeaderboardRowDTO
                {
                    Rank = i + 1,
                    CustomerId = (int)r.CustomerId,
                    CustomerName = (string)r.CustomerName,
                    Revenue = (decimal)r.Total,
                    InvoiceCount = (int)r.InvoiceCount,
                    Category = (string)r.Category
                }).ToList();

            // ── Charts: Revenue by Customer Category ─────────────────────────
            const string categoryDistQuery = @"
                SELECT ISNULL(cat.category_name, 'Others') AS Category, SUM(si.grand_total) AS Value
                FROM sales_invoice si
                JOIN master_customer c ON c.customer_id = si.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE si.invoice_date BETWEEN @From AND @To
                  AND (@CustomerId IS NULL OR si.customer_id = @CustomerId)
                  AND (@Category IS NULL OR cat.category_name = @Category)
                GROUP BY cat.category_name
                ORDER BY SUM(si.grand_total) DESC";

            result.Charts.CategoryDistribution = (await connection.QueryAsync<CategoryRevenueDTO>(categoryDistQuery,
                new { q.From, q.To, q.CustomerId, q.Category })).ToList();

            // ── Charts: Outstanding Receivable Trend (monthly) ───────────────
            const string outstandingTrendQuery = @"
                SELECT
                    CAST(YEAR(si.invoice_date) AS VARCHAR) + '-' + RIGHT('0' + CAST(MONTH(si.invoice_date) AS VARCHAR), 2) AS MonthKey,
                    SUM(si.remaining_amount) AS Value
                FROM sales_invoice si
                JOIN master_customer c ON c.customer_id = si.customer_id
                LEFT JOIN master_customer_category cat ON cat.id = c.category_id
                WHERE si.invoice_date BETWEEN @From AND @To
                  AND si.status NOT IN ('Paid', 'Cancelled')
                  AND (@CustomerId IS NULL OR si.customer_id = @CustomerId)
                  AND (@Category IS NULL OR cat.category_name = @Category)
                GROUP BY YEAR(si.invoice_date), MONTH(si.invoice_date)";

            var outstandingByMonth = (await connection.QueryAsync(outstandingTrendQuery,
                new { q.From, q.To, q.CustomerId, q.Category }))
                .ToDictionary(r => (string)r.MonthKey, r => (decimal)r.Value);
            result.Charts.OutstandingTrend = BuildMonthlyPoints(outstandingByMonth, q.From, q.To);

            // ── Charts: Return Trend (monthly) ───────────────────────────────
            const string returnTrendQuery = @"
                SELECT
                    CAST(YEAR(return_date) AS VARCHAR) + '-' + RIGHT('0' + CAST(MONTH(return_date) AS VARCHAR), 2) AS MonthKey,
                    COUNT(*) AS Value
                FROM sales_return_header
                WHERE return_date BETWEEN @From AND @To
                GROUP BY YEAR(return_date), MONTH(return_date)";

            var returnByMonth = (await connection.QueryAsync(returnTrendQuery,
                new { q.From, q.To }))
                .ToDictionary(r => (string)r.MonthKey, r => (decimal)(int)r.Value);
            result.Charts.ReturnTrend = BuildMonthlyPoints(returnByMonth, q.From, q.To);

            // ── Funnel (matches old frontend semantics exactly: Quotation/SO/
            // Invoiced/Paid counts are ALL-TIME, only "Delivered" is date-ranged) ─
            var quotationCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sales_quotation");
            var soAllCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sales_order WHERE status <> 'Cancelled'");
            var deliveredInRange = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM delivery_order_header WHERE do_date BETWEEN @From AND @To",
                new { q.From, q.To });
            var invoicedCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sales_invoice WHERE status <> 'Cancelled'");
            var paidCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sales_invoice WHERE status = 'Paid'");

            result.Funnel = new List<SalesFunnelStepDTO>
            {
                new() { Label = "Quotation", Count = quotationCount },
                new() { Label = "Sales Order", Count = soAllCount },
                new() { Label = "Delivered", Count = deliveredInRange },
                new() { Label = "Invoiced", Count = invoicedCount },
                new() { Label = "Paid", Count = paidCount },
            };

            return result;
        }

        // Fills every month between from/to (inclusive) with 0 where no row
        // was aggregated, so charts don't show gaps -- mirrors the old
        // frontend buildMonthlyPoints helper exactly (max 24 buckets guard).
        private static List<MonthPointDTO> BuildMonthlyPoints(Dictionary<string, decimal> byMonth, DateTime from, DateTime to)
        {
            var result = new List<MonthPointDTO>();
            var cursor = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            int guard = 0;
            while (cursor <= end && guard < 24)
            {
                var key = $"{cursor.Year}-{cursor.Month:D2}";
                var label = cursor.ToString("MMM yy", new System.Globalization.CultureInfo("id-ID"));
                result.Add(new MonthPointDTO
                {
                    Month = label,
                    Value = byMonth.TryGetValue(key, out var v) ? v : 0
                });
                cursor = cursor.AddMonths(1);
                guard++;
            }

            return result;
        }
    }
}
