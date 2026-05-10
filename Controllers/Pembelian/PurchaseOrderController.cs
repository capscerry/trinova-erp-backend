using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        [HttpGet]
        public IActionResult GetPurchaseOrders()
        {
            var purchaseOrders = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .ThenInclude(s => s.SupplierCategory)
                .ToList();

            return Ok(purchaseOrders);
        }

        // GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetPurchaseOrderById(int id)
        {
            var purchaseOrder = _context.PurchaseOrders
                .Include(p => p.Supplier)
                .ThenInclude(s => s.SupplierCategory)
                .FirstOrDefault(p => p.purchase_order_id == id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return Ok(purchaseOrder);
        }

        // CREATE
        [HttpPost]
        public IActionResult CreatePurchaseOrder(PurchaseOrder purchaseOrder)
        {
            purchaseOrder.created_at = DateTime.Now;

            // DEFAULT STATUS
            purchaseOrder.status = "Draft";

            // AUTO GENERATE PO NUMBER
            string today = DateTime.Now.ToString("yyyyMMdd");

            int countToday = _context.PurchaseOrders
                .Count(p => p.created_at.HasValue &&
                            p.created_at.Value.Date == DateTime.Today);

            purchaseOrder.po_number =
                $"PO-{today}-{(countToday + 1).ToString("D3")}";

            _context.PurchaseOrders.Add(purchaseOrder);

            _context.SaveChanges();

            return Ok(purchaseOrder);
        }

        // UPDATE
        [HttpPut("{id}")]
        public IActionResult UpdatePurchaseOrder(int id, PurchaseOrder updatedPurchaseOrder)
        {
            var purchaseOrder = _context.PurchaseOrders.Find(id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            // BLOCK EDIT IF COMPLETED
            if (purchaseOrder.status == "Completed")
            {
                return BadRequest("Completed PO cannot be edited");
            }

            purchaseOrder.supplier_id = updatedPurchaseOrder.supplier_id;
            purchaseOrder.order_date = updatedPurchaseOrder.order_date;
            purchaseOrder.status = updatedPurchaseOrder.status;
            purchaseOrder.total_amount = updatedPurchaseOrder.total_amount;

            _context.SaveChanges();

            return Ok(purchaseOrder);
        }

        // DELETE
        [HttpDelete("{id}")]
        public IActionResult DeletePurchaseOrder(int id)
        {
            var purchaseOrder = _context.PurchaseOrders.Find(id);

            if (purchaseOrder == null)
            {
                return NotFound();
            }

            // BLOCK DELETE IF APPROVED
            if (purchaseOrder.status == "Approved")
            {
                return BadRequest("Approved PO cannot be deleted");
            }

            _context.PurchaseOrders.Remove(purchaseOrder);

            _context.SaveChanges();

            return Ok("Purchase Order deleted");
        }
    }
}