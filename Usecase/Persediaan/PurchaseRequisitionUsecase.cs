using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class PurchaseRequisitionUsecase
    {
        private readonly PurchaseRequisitionRepo _purchaseRequisitionRepo;

        public PurchaseRequisitionUsecase(
            PurchaseRequisitionRepo purchaseRequisitionRepo
        )
        {
            _purchaseRequisitionRepo = purchaseRequisitionRepo;
        }

        public async Task<string> GetNextPRNumber()
        {
            return await _purchaseRequisitionRepo
                .GeneratePrNumber();
        }

        public async Task<List<PurchaseRequisition>> GetAllAsync()
        {
            return await _purchaseRequisitionRepo.GetAllAsync();
        }

        public async Task<PurchaseRequisition?> GetByIdAsync(int id)
        {
            return await _purchaseRequisitionRepo.GetByIdAsync(id);
        }

        public async Task<PurchaseRequisition> CreateAsync(
            PurchaseRequisition model
        )
        {
            model.pr_number =
                await _purchaseRequisitionRepo.GeneratePrNumber();

            model.status = "REQUESTED";

            model.created_at = DateTime.Now;

            foreach (var detail in model.Details)
            {
                detail.qty_processed = 0;
            }

            return await _purchaseRequisitionRepo.CreateAsync(model);
        }

        public async Task<bool> UpdateAsync(
            int id,
            PurchaseRequisition model
        )
        {
            var existing =
                await _purchaseRequisitionRepo.GetByIdAsync(id);

            if (existing == null)
                return false;

            existing.pr_date = model.pr_date;
            existing.warehouse_id = model.warehouse_id;
            existing.remarks = model.remarks;
            existing.updated_at = DateTime.Now;

            await _purchaseRequisitionRepo.UpdateAsync(existing);

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing =
                await _purchaseRequisitionRepo.GetByIdAsync(id);

            if (existing == null)
                return false;

            await _purchaseRequisitionRepo.DeleteAsync(existing);

            return true;
        }
    }
}