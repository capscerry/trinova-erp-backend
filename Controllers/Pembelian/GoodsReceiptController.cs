using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoodsReceiptController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GoodsReceiptController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        [HttpGet]
        public IActionResult GetGoodsReceipts()
        {
            var receipts = _context.GoodsReceipts
                .Include(g => g.PurchaseOrder)
                    .ThenInclude(p => p.Supplier)
                        .ThenInclude(s => s.SupplierCategory)
                .ToList();

            return Ok(receipts);
        }

        // GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetGoodsReceiptById(int id)
        {
            var receipt = _context.GoodsReceipts
                .Include(g => g.PurchaseOrder)
                    .ThenInclude(p => p.Supplier)
                        .ThenInclude(s => s.SupplierCategory)
                .FirstOrDefault(g => g.goods_receipt_id == id);

            if (receipt == null)
            {
                return NotFound();
            }

            return Ok(receipt);
        }

        // CREATE
        [HttpPost]
        public IActionResult CreateGoodsReceipt(GoodsReceipt goodsReceipt)
        {
            goodsReceipt.created_at = DateTime.Now;

            // AUTO RECEIPT NUMBER
            string today = DateTime.Now.ToString("yyyyMMdd");

            int countToday = _context.GoodsReceipts
                .Count(g => g.created_at.HasValue &&
                            g.created_at.Value.Date == DateTime.Today);

            goodsReceipt.receipt_number =
                $"GR-{today}-{(countToday + 1).ToString("D3")}";

            // DEFAULT STATUS
            goodsReceipt.status = "Received";

            _context.GoodsReceipts.Add(goodsReceipt);

            // AUTO COMPLETE PO
            var purchaseOrder = _context.PurchaseOrders
                .FirstOrDefault(p => p.purchase_order_id == goodsReceipt.purchase_order_id);

            if (purchaseOrder != null)
            {
                purchaseOrder.status = "Completed";
            }

            _context.SaveChanges();

            return Ok(goodsReceipt);
        }

        // DELETE
        [HttpDelete("{id}")]
        public IActionResult DeleteGoodsReceipt(int id)
        {
            var receipt = _context.GoodsReceipts.Find(id);

            if (receipt == null)
            {
                return NotFound();
            }

            _context.GoodsReceipts.Remove(receipt);

            _context.SaveChanges();

            return Ok("Goods Receipt deleted");
        }
    }
}