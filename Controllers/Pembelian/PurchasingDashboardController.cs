using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Data;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/purchasing/dashboard")]
    public class PurchasingDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PurchasingDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetDashboard()
        {
            var totalSupplier = _context.Suppliers.Count();

            var totalPurchaseOrder = _context.PurchaseOrders.Count();

            var totalGoodsReceipt = _context.GoodsReceipts.Count();

            var totalPurchaseAmount = _context.PurchaseOrders
                .Sum(p => p.total_amount) ?? 0;

            // PO STATUS SUMMARY
            var draftCount = _context.PurchaseOrders
                .Count(p => p.status == "Draft");

            var approvedCount = _context.PurchaseOrders
                .Count(p => p.status == "Approved");

            var completedCount = _context.PurchaseOrders
                .Count(p => p.status == "Completed");

            // TOP SUPPLIER ANALYTICS
            var topSupplier = _context.PurchaseOrders
                .GroupBy(p => p.Supplier.supplier_name)
                .Select(g => new
                {
                    supplier_name = g.Key,
                    total_po = g.Count(),
                    total_purchase_amount = g.Sum(x => x.total_amount)
                })
                .OrderByDescending(x => x.total_purchase_amount)
                .FirstOrDefault();

            // SUPPLIER SCORING
            var supplierScoring = _context.PurchaseOrders
                .GroupBy(p => p.Supplier.supplier_name)
                .Select(g => new
                {
                    supplier_name = g.Key,

                    total_po = g.Count(),

                    total_amount = g.Sum(x => x.total_amount) ?? 0,

                    completed_po = g.Count(x => x.status == "Completed")
                })
                .ToList()
                .Select(s => new
                {
                    supplier_name = s.supplier_name,

                    score =
                        (s.total_po * 10) +
                        ((int)s.total_amount / 1000000) +
                        (s.completed_po * 20),

                    recommendation =
                        ((s.total_po * 10) +
                        ((int)s.total_amount / 1000000) +
                        (s.completed_po * 20)) >= 80
                        ? "Recommended Supplier"
                        : "Average Supplier"
                });

            var dashboard = new
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

            return Ok(dashboard);
        }
    }
}