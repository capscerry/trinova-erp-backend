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
            int totalSupplier = 0;
            int totalPurchaseOrder = 0;
            int totalGoodsReceipt = 0;
            decimal totalPurchaseAmount = 0;

            int draftCount = 0;
            int approvedCount = 0;
            int completedCount = 0;

            object? topSupplier = null;

            var supplierScoring = new List<object>();

            try
            {
                using (
                    SqlConnection connection = new SqlConnection(
                        _connectionString
                    )
                )
                {
                    await connection.OpenAsync();

                    // TOTAL SUPPLIER
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM master_supplier",
                            connection
                        )
                    )
                    {
                        totalSupplier = Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // TOTAL PO
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM purchase_order",
                            connection
                        )
                    )
                    {
                        totalPurchaseOrder = Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // TOTAL GOODS RECEIPT
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM goods_receipt",
                            connection
                        )
                    )
                    {
                        totalGoodsReceipt = Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // TOTAL PURCHASE AMOUNT
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT ISNULL(SUM(total_amount),0) FROM purchase_order",
                            connection
                        )
                    )
                    {
                        totalPurchaseAmount = Convert.ToDecimal(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // DRAFT COUNT
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM purchase_order WHERE status = 'Draft'",
                            connection
                        )
                    )
                    {
                        draftCount = Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // APPROVED COUNT
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM purchase_order WHERE status = 'Approved'",
                            connection
                        )
                    )
                    {
                        approvedCount = Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // COMPLETED COUNT
                    using (
                        SqlCommand command = new SqlCommand(
                            "SELECT COUNT(*) FROM purchase_order WHERE status = 'Completed'",
                            connection
                        )
                    )
                    {
                        completedCount = Convert.ToInt32(
                            await command.ExecuteScalarAsync()
                        );
                    }

                    // TOP SUPPLIER
                    const string topSupplierQuery = @"
                        SELECT TOP 1
                            s.supplier_name,
                            COUNT(po.purchase_order_id) AS total_po,
                            ISNULL(SUM(po.total_amount),0) AS total_purchase_amount
                        FROM purchase_order po
                        LEFT JOIN master_supplier s
                            ON po.supplier_id = s.supplier_id
                        GROUP BY s.supplier_name
                        ORDER BY total_purchase_amount DESC";

                    using (
                        SqlCommand command = new SqlCommand(
                            topSupplierQuery,
                            connection
                        )
                    )
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            topSupplier = new
                            {
                                supplier_name = reader["supplier_name"].ToString(),
                                total_po = Convert.ToInt32(reader["total_po"]),
                                total_purchase_amount = Convert.ToDecimal(
                                    reader["total_purchase_amount"]
                                )
                            };
                        }
                    }

                    // SUPPLIER SCORING
                    const string scoringQuery = @"
                        SELECT
                            s.supplier_name,
                            COUNT(po.purchase_order_id) AS total_po,
                            ISNULL(SUM(po.total_amount),0) AS total_amount,
                            SUM(
                                CASE
                                    WHEN po.status = 'Completed'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS completed_po
                        FROM purchase_order po
                        LEFT JOIN master_supplier s
                            ON po.supplier_id = s.supplier_id
                        GROUP BY s.supplier_name";

                    using (
                        SqlCommand command = new SqlCommand(
                            scoringQuery,
                            connection
                        )
                    )
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int totalPo = Convert.ToInt32(reader["total_po"]);
                            decimal totalAmount = Convert.ToDecimal(
                                reader["total_amount"]
                            );
                            int completedPo = Convert.ToInt32(
                                reader["completed_po"]
                            );

                            decimal score =
                                (totalPo * 10m)
                                + (totalAmount / 1000000m)
                                + (completedPo * 20m);

                            supplierScoring.Add(
                                new
                                {
                                    supplier_name = reader["supplier_name"]
                                        .ToString(),

                                    score = Math.Round(score, 2),

                                    recommendation =
                                        score >= 80m
                                        ? "Recommended Supplier"
                                        : "Average Supplier"
                                }
                            );
                        }
                    }
                }

                return new
                {
                    total_supplier = totalSupplier,

                    total_purchase_order = totalPurchaseOrder,

                    total_goods_receipt = totalGoodsReceipt,

                    total_purchase_amount = totalPurchaseAmount,

                    po_status_summary = new
                    {
                        draft = draftCount,
                        approved = approvedCount,
                        completed = completedCount
                    },

                    top_supplier = topSupplier,

                    supplier_scoring = supplierScoring
                };
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                throw new Exception(msg);
            }
        }
    }
}