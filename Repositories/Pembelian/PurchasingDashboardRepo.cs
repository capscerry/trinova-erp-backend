using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchasingDashboardRepo
    {
        Task<object> GetDashboard();
    }

    public class PurchasingDashboardRepo : IPurchasingDashboardRepo
    {
        private readonly string _connectionString;

        public PurchasingDashboardRepo(
            IOptionsSnapshot<DatabaseConnection> options
        )
        {
            _connectionString = options.Value.SQLServer
                ?? throw new InvalidOperationException(
                    "Database connection string is not configured."
                );
        }

        public async Task<object> GetDashboard()
        {
            int     totalSupplier       = 0;
            int     totalPurchaseOrder  = 0;
            int     totalGoodsReceipt   = 0;
            decimal totalPurchaseAmount = 0;

            int draftCount     = 0;
            int approvedCount  = 0;
            int completedCount = 0;

            object? topSupplier = null;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // TOTAL SUPPLIER
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM master_supplier", connection))
                        totalSupplier = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // TOTAL PO
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM purchase_order", connection))
                        totalPurchaseOrder = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // TOTAL GOODS RECEIPT
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM goods_receipt", connection))
                        totalGoodsReceipt = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // TOTAL PURCHASE AMOUNT
                    using (var command = new SqlCommand("SELECT ISNULL(SUM(total_amount),0) FROM purchase_order", connection))
                        totalPurchaseAmount = Convert.ToDecimal(await command.ExecuteScalarAsync());

                    // PO STATUS COUNTS
                    using (var command = new SqlCommand("SELECT COUNT(*) FROM purchase_order WHERE status = 'Draft'", connection))
                        draftCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM purchase_order WHERE status = 'Approved'", connection))
                        approvedCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                    using (var command = new SqlCommand("SELECT COUNT(*) FROM purchase_order WHERE status = 'Completed'", connection))
                        completedCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                    // TOP SUPPLIER (by total purchase amount — display metric only)
                    const string topSupplierQuery = @"
                        SELECT TOP 1
                            s.supplier_name,
                            COUNT(po.purchase_order_id) AS total_po,
                            ISNULL(SUM(po.total_amount),0) AS total_purchase_amount
                        FROM purchase_order po
                        LEFT JOIN master_supplier s ON po.supplier_id = s.supplier_id
                        GROUP BY s.supplier_name
                        ORDER BY total_purchase_amount DESC";

                    using (var command = new SqlCommand(topSupplierQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            topSupplier = new
                            {
                                supplier_name         = reader["supplier_name"].ToString(),
                                total_po              = Convert.ToInt32(reader["total_po"]),
                                total_purchase_amount = Convert.ToDecimal(reader["total_purchase_amount"])
                            };
                        }
                    }
                }

                return new
                {
                    total_supplier        = totalSupplier,
                    total_purchase_order  = totalPurchaseOrder,
                    total_goods_receipt   = totalGoodsReceipt,
                    total_purchase_amount = totalPurchaseAmount,

                    po_status_summary = new
                    {
                        draft     = draftCount,
                        approved  = approvedCount,
                        completed = completedCount
                    },

                    top_supplier = topSupplier
                    // NOTE: supplier_scoring and recommendation data are now provided
                    // by ISupplierRiskUsecase.GetRecommendation() via the usecase layer
                    // so that every screen shares the exact same AHP-TOPSIS result.
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}