using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Controllers.Pembelian
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierCategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SupplierCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = _context.SupplierCategories.ToList();

            return Ok(categories);
        }

        // GET BY ID
        [HttpGet("{id}")]
        public IActionResult GetCategoryById(int id)
        {
            var category = _context.SupplierCategories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            return Ok(category);
        }

        // CREATE
        [HttpPost]
        public IActionResult CreateCategory(SupplierCategory category)
        {
            category.created_date = DateTime.Now;

            _context.SupplierCategories.Add(category);

            _context.SaveChanges();

            return Ok(category);
        }

        // UPDATE
        [HttpPut("{id}")]
        public IActionResult UpdateCategory(int id, SupplierCategory updatedCategory)
        {
            var category = _context.SupplierCategories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            category.category_name = updatedCategory.category_name;
            category.update_date = DateTime.Now;
            category.update_by = updatedCategory.update_by;

            _context.SaveChanges();

            return Ok(category);
        }

        // DELETE
        [HttpDelete("{id}")]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.SupplierCategories.Find(id);

            if (category == null)
            {
                return NotFound();
            }

            _context.SupplierCategories.Remove(category);

            _context.SaveChanges();

            return Ok("Category deleted");
        }
    }
}