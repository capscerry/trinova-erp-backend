using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        [HttpGet]
        public IActionResult GetSuppliers()
        {
            var suppliers = _context.Suppliers
                .Include(s => s.SupplierCategory)
                .ToList();

            return Ok(suppliers);
        }

        // GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetSupplierById(int id)
        {
            var supplier = _context.Suppliers
                .Include(s => s.SupplierCategory)
                .FirstOrDefault(s => s.supplier_id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return Ok(supplier);
        }

        // CREATE
        [HttpPost]
        public IActionResult CreateSupplier(Supplier supplier)
        {
            supplier.created_date = DateTime.Now;

            _context.Suppliers.Add(supplier);

            _context.SaveChanges();

            return Ok(supplier);
        }

        // UPDATE
        [HttpPut("{id}")]
        public IActionResult UpdateSupplier(int id, Supplier updatedSupplier)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
            {
                return NotFound();
            }

            supplier.supplier_code = updatedSupplier.supplier_code;
            supplier.supplier_name = updatedSupplier.supplier_name;
            supplier.category_supplier = updatedSupplier.category_supplier;
            supplier.no_telp_bisnis = updatedSupplier.no_telp_bisnis;
            supplier.no_telp_wa = updatedSupplier.no_telp_wa;
            supplier.alamat = updatedSupplier.alamat;
            supplier.email = updatedSupplier.email;
            supplier.faximili = updatedSupplier.faximili;
            supplier.website = updatedSupplier.website;
            supplier.status = updatedSupplier.status;
            supplier.type_supplier = updatedSupplier.type_supplier;
            supplier.update_date = DateTime.Now;
            supplier.update_by = updatedSupplier.update_by;

            _context.SaveChanges();

            return Ok(supplier);
        }

        // DELETE
        [HttpDelete("{id}")]
        public IActionResult DeleteSupplier(int id)
        {
            var supplier = _context.Suppliers.Find(id);

            if (supplier == null)
            {
                return NotFound();
            }

            _context.Suppliers.Remove(supplier);

            _context.SaveChanges();

            return Ok("Supplier deleted");
        }
    }
}