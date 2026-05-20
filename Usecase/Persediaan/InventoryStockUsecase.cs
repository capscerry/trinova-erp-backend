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

        //public async Task<List<InventoryStock>> GetAllAsync()
        //{
        //    return await _repo.GetAllAsync();
        //}

        //public async Task<InventoryStock?> GetByIdAsync(int id)
        //{
        //    return await _repo.GetByIdAsync(id);
        //}

        //public async Task<InventoryStock> CreateAsync(InventoryStock stock)
        //{
        //    if (stock.quantity < 0)
        //        throw new Exception("Quantity cannot be negative");

        //    return await _repo.CreateAsync(stock);
        //}

        //public async Task UpdateAsync(InventoryStock stock)
        //{
        //    await _repo.UpdateAsync(stock);
        //}

        //public async Task DeleteAsync(InventoryStock stock)
        //{
        //    await _repo.DeleteAsync(stock);
        //}
    }
}