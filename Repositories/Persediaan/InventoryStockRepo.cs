using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class InventoryStockRepo
    {
        //private readonly ApplicationDbContext _context;
        private readonly string _context;

        public InventoryStockRepo(IOptions<DatabaseConnection> options)
        {
            _context = options.Value.SQLServer;
        }

        //public async Task<List<InventoryStock>> GetAllAsync()
        //{
        //    return await _context.InventoryStocks.ToListAsync();
        //}

        //public async Task<InventoryStock?> GetByIdAsync(int id)
        //{
        //    return await _context.InventoryStocks.FindAsync(id);
        //}

        //public async Task<InventoryStock> CreateAsync(InventoryStock stock)
        //{
        //    _context.InventoryStocks.Add(stock);
        //    await _context.SaveChangesAsync();
        //    return stock;
        //}

        //public async Task UpdateAsync(InventoryStock stock)
        //{
        //    _context.InventoryStocks.Update(stock);
        //    await _context.SaveChangesAsync();
        //}

        //public async Task DeleteAsync(InventoryStock stock)
        //{
        //    _context.InventoryStocks.Remove(stock);
        //    await _context.SaveChangesAsync();
        //}
    }
}