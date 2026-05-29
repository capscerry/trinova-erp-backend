using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterProductSubcategoryRepo
    {
        private readonly ApplicationDbContext _context;

        public MasterProductSubcategoryRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductSubcategory>> GetAllAsync()
        {
            return await _context.MasterProductSubcategories
                .Include(x => x.Category)
                .ToListAsync();
        }

        public async Task<List<ProductSubcategory>> GetByCategoryAsync(int categoryId)
        {
            return await _context.MasterProductSubcategories
                .Include(x => x.Category)
                .Where(x => x.category_id == categoryId)
                .ToListAsync();
        }

        public async Task<ProductSubcategory?> GetByIdAsync(int id)
        {
            return await _context.MasterProductSubcategories
                .FirstOrDefaultAsync(x => x.subcategory_id == id);
        }

        public async Task<ProductSubcategory?> GetByNameAsync(
            string name
        )
        {
            return await _context
                .MasterProductSubcategories
                .FirstOrDefaultAsync(
                    x => x.name == name
                );
        }

        public async Task<ProductSubcategory> CreateAsync(ProductSubcategory subcategory)
        {
            _context.MasterProductSubcategories.Add(subcategory);

            await _context.SaveChangesAsync();

            return subcategory;
        }

        public async Task<ProductSubcategory?> UpdateAsync(ProductSubcategory subcategory)
        {
            _context.MasterProductSubcategories.Update(subcategory);

            await _context.SaveChangesAsync();

            return subcategory;
        }

        public async Task<bool> DeleteAsync(ProductSubcategory subcategory)
        {
            _context.MasterProductSubcategories.Remove(subcategory);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}