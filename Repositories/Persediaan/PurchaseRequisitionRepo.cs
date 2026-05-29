using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class PurchaseRequisitionRepo
    {
        private readonly ApplicationDbContext _context;

        public PurchaseRequisitionRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseRequisition>> GetAllAsync()
        {
            return await _context.PurchaseRequisitions
                .Include(x => x.Warehouse)
                .Include(x => x.Details)
                .OrderByDescending(x => x.created_at)
                .ToListAsync();
        }

        public async Task<PurchaseRequisition?> GetByIdAsync(int id)
        {
            return await _context.PurchaseRequisitions
                .Include(x => x.Warehouse)
                .Include(x => x.Details)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.pr_id == id);
        }

        public async Task<PurchaseRequisition> CreateAsync(
            PurchaseRequisition requisition
        )
        {
            _context.PurchaseRequisitions.Add(requisition);

            await _context.SaveChangesAsync();

            return requisition;
        }

        public async Task UpdateAsync(
            PurchaseRequisition requisition
        )
        {
            _context.PurchaseRequisitions.Update(requisition);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(
            PurchaseRequisition requisition
        )
        {
            _context.PurchaseRequisitions.Remove(requisition);

            await _context.SaveChangesAsync();
        }

        public async Task<string> GeneratePrNumber()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");

            int countToday = await _context.PurchaseRequisitions
                .CountAsync(x =>
                    x.created_at.Date == DateTime.Today);

            return $"PR-{today}-{(countToday + 1):D4}";
        }
    }
}