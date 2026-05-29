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
                {
                    throw new Exception(
                        "Stock cannot be negative"
                    );
                }

                stock.qty_available =
                    stock.qty_on_hand -
                    stock.qty_reserved;

                stock.created_at =
                    DateTime.Now;

                stock.updated_at =
                    DateTime.Now;
            return await _repo.CreateAsync(stock);
        }

        public async Task UpdateAsync(InventoryStock stock)
        {
            await _repo.UpdateAsync(stock);
        }

        public async Task DeleteAsync(InventoryStock stock)
        {
            await _repo.DeleteAsync(stock);
        }
    }
}