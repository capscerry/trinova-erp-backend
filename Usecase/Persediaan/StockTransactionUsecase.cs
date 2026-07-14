using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class StockTransactionUsecase
    {
        private readonly StockTransactionRepo _repo;

        public StockTransactionUsecase(StockTransactionRepo repo)
        {
            _repo = repo;
        }

        public async Task<List<StockTransaction>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<StockTransaction?> GetByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<StockTransaction> CreateAsync(
            StockTransaction transaction
        )
        {
            if (
                transaction.transaction_type != "IN" &&
                transaction.transaction_type != "OUT" &&
                transaction.transaction_type != "TRANSFER_IN" &&
                transaction.transaction_type != "TRANSFER_OUT"&&
                transaction.transaction_type != "ADJUSTMENT"
            )
            {
                throw new Exception("Invalid transaction type");
            }

            if (transaction.quantity <= 0)
            {
                throw new Exception(
                    "Quantity must be greater than zero"
                );
            }

            transaction.created_at = DateTime.Now;

            return await _repo.CreateAsync(transaction);
        }
    }
}