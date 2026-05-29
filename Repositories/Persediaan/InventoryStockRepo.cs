using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class InventoryStockRepo
    {
        private readonly ApplicationDbContext _context;

        public InventoryStockRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryStock>> GetAllAsync()
        {
            return await _context.InventoryStocks
                .Include(x => x.Product!)
                .ThenInclude(x => x.ProductSubcategory)
                .Include(x => x.Warehouse)
                .ToListAsync();
        }

        public async Task<InventoryStock?> GetByIdAsync(int id)
        {
            return await _context.InventoryStocks
                .Include(x => x.Product!)
                .ThenInclude(x => x.ProductSubcategory)
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x =>
                    x.stock_id == id
                );
        }

        public async Task<InventoryStock> CreateAsync(InventoryStock stock)
        {
            _context.InventoryStocks.Add(stock);
            await _context.SaveChangesAsync();
            return stock;
        }

        public async Task UpdateAsync(InventoryStock stock)
        {
            _context.InventoryStocks.Update(stock);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(InventoryStock stock)
        {
            _context.InventoryStocks.Remove(stock);
            await _context.SaveChangesAsync();
        }
    }
}