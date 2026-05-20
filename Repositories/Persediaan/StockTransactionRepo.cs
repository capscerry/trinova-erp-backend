using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class StockTransactionRepo
    {
        //private readonly ApplicationDbContext _context;
        private readonly string _context;

        public StockTransactionRepo(IOptions<DatabaseConnection> options)
        {
            _context = options.Value.SQLServer;
        }

        //public async Task<List<StockTransaction>> GetAllAsync()
        //{
        //    return await _context.StockTransactions.ToListAsync();
        //}

        //public async Task<StockTransaction?> GetByIdAsync(int id)
        //{
        //    return await _context.StockTransactions.FindAsync(id);
        //}

        //public async Task<StockTransaction> CreateAsync(StockTransaction transaction)
        //{
        //    _context.StockTransactions.Add(transaction);
        //    await _context.SaveChangesAsync();
        //    return transaction;
        //}
    }
}