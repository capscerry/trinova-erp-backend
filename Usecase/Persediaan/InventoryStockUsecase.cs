using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class InventoryStockUsecase
    {
        private readonly InventoryStockRepo _repo;

        public InventoryStockUsecase(InventoryStockRepo repo)
        {
            _repo = repo;
        }

        public async Task<List<InventoryStock>> GetAllAsync()
        {
           return await _repo.GetAllAsync();
        }

        public async Task<InventoryStock?> GetByIdAsync(int id)
        {
           return await _repo.GetByIdAsync(id);
        }

        public async Task<InventoryStock> CreateAsync(InventoryStock stock)
        {
            if (stock.qty_on_hand < 0)
                throw new Exception("Stock cannot be negative");

            stock.qty_available = stock.qty_on_hand - stock.qty_reserved;
            stock.created_at    = DateTime.UtcNow.AddHours(7);
            stock.updated_at    = DateTime.UtcNow.AddHours(7);

            var result = await _repo.CreateAsync(stock);

            // Keep supplier_products.available_stock in sync so the PO form
            // always reflects the current inventory level.
            await _repo.SyncToSupplierProductsAsync(stock.product_id);

            return result;
        }

        public async Task UpdateAsync(InventoryStock stock)
        {
            if (stock.qty_on_hand < 0)
                throw new Exception("Stock cannot be negative");

            stock.qty_available = stock.qty_on_hand - stock.qty_reserved;
            stock.updated_at    = DateTime.UtcNow.AddHours(7);

            await _repo.UpdateAsync(stock);

            // Sync the updated total back to supplier_products.
            await _repo.SyncToSupplierProductsAsync(stock.product_id);
        }

        public async Task DeleteAsync(InventoryStock stock)
        {
            await _repo.DeleteAsync(stock);

            // After deletion the total available may drop — re-sync.
            await _repo.SyncToSupplierProductsAsync(stock.product_id);
        }
    }
}