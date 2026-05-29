using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class StockTransactionRepo
    {
        private readonly ApplicationDbContext _context;

        public StockTransactionRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockTransaction>> GetAllAsync()
        {
            return await _context.StockTransactions
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .OrderByDescending(x => x.created_at)
                .ToListAsync();
        }

        public async Task<StockTransaction?> GetByIdAsync(int id)
        {
            return await _context.StockTransactions
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x => x.transaction_id == id);
        }

        public async Task<StockTransaction> CreateAsync(StockTransaction transaction)
        {
            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }
    }
}