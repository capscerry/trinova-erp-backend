using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrderDetailController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderDetailController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        [HttpGet]
        public IActionResult GetPurchaseOrderDetails()
        {
            var details = _context.PurchaseOrderDetails
                .Include(d => d.PurchaseOrder)
                    .ThenInclude(p => p.Supplier)
                        .ThenInclude(s => s.SupplierCategory)
                .ToList();

            return Ok(details);
        }

        // GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetPurchaseOrderDetailById(int id)
        {
            var detail = _context.PurchaseOrderDetails
                .Include(d => d.PurchaseOrder)
                    .ThenInclude(p => p.Supplier)
                        .ThenInclude(s => s.SupplierCategory)
                .FirstOrDefault(d => d.purchase_order_detail_id == id);

            if (detail == null)
            {
                return NotFound();
            }

            return Ok(detail);
        }

        // CREATE
        [HttpPost]
        public IActionResult CreatePurchaseOrderDetail(PurchaseOrderDetail detail)
        {
            // AUTO SUBTOTAL
            detail.subtotal = detail.quantity * detail.price;

            _context.PurchaseOrderDetails.Add(detail);

            _context.SaveChanges();

            // AUTO UPDATE PO TOTAL
            UpdatePurchaseOrderTotal(detail.purchase_order_id);

            return Ok(detail);
        }

        // UPDATE
        [HttpPut("{id}")]
        public IActionResult UpdatePurchaseOrderDetail(int id, PurchaseOrderDetail updatedDetail)
        {
            var detail = _context.PurchaseOrderDetails.Find(id);

            if (detail == null)
            {
                return NotFound();
            }

            detail.product_name = updatedDetail.product_name;
            detail.quantity = updatedDetail.quantity;
            detail.price = updatedDetail.price;

            // AUTO RECALCULATE SUBTOTAL
            detail.subtotal = updatedDetail.quantity * updatedDetail.price;

            _context.SaveChanges();

            // AUTO UPDATE PO TOTAL
            UpdatePurchaseOrderTotal(detail.purchase_order_id);

            return Ok(detail);
        }

        // DELETE
        [HttpDelete("{id}")]
        public IActionResult DeletePurchaseOrderDetail(int id)
        {
            var detail = _context.PurchaseOrderDetails.Find(id);

            if (detail == null)
            {
                return NotFound();
            }

            int purchaseOrderId = detail.purchase_order_id;

            _context.PurchaseOrderDetails.Remove(detail);

            _context.SaveChanges();

            // AUTO UPDATE PO TOTAL
            UpdatePurchaseOrderTotal(purchaseOrderId);

            return Ok("Purchase Order Detail deleted");
        }

        // AUTO CALCULATE TOTAL
        private void UpdatePurchaseOrderTotal(int purchaseOrderId)
        {
            var purchaseOrder = _context.PurchaseOrders
                .FirstOrDefault(p => p.purchase_order_id == purchaseOrderId);

            if (purchaseOrder != null)
            {
                purchaseOrder.total_amount = _context.PurchaseOrderDetails
                    .Where(d => d.purchase_order_id == purchaseOrderId)
                    .Sum(d => d.subtotal);

                _context.SaveChanges();
            }
        }
    }
}