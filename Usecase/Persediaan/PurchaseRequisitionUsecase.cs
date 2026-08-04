using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class PurchaseRequisitionUsecase
    {

        private readonly PurchaseRequisitionDetailRepo _purchaseRequisitionDetailRepo;
        private readonly PurchaseRequisitionRepo _purchaseRequisitionRepo;

        public PurchaseRequisitionUsecase(
            PurchaseRequisitionRepo purchaseRequisitionRepo,
            PurchaseRequisitionDetailRepo purchaseRequisitionDetailRepo
        )
        {
            _purchaseRequisitionRepo = purchaseRequisitionRepo;
            _purchaseRequisitionDetailRepo = purchaseRequisitionDetailRepo;
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

        public async Task<PurchaseRequisition?> GetDetailAsync(int id)
        {
            return await _purchaseRequisitionRepo.GetDetailAsync(id);
        }

        public async Task<PurchaseRequisition> CreateAsync(
            PurchaseRequisition model
        )
        {
            model.pr_number =
                await _purchaseRequisitionRepo.GeneratePrNumber();

            model.status = "REQUESTED";

            model.created_at = DateTime.UtcNow.AddHours(7);

            foreach (var detail in model.Details)
            {
                detail.qty_processed = 0;
            }

            var createdPr =
                await _purchaseRequisitionRepo.CreateAsync(model);

            Console.WriteLine($"HEADER ID = {createdPr.pr_id}");

            foreach (var detail in model.Details)
            {
                Console.WriteLine(
                    $"Saving Product={detail.product_id}, Qty={detail.qty_requested}"
                );

                detail.pr_id = createdPr.pr_id;
                detail.qty_processed = 0;

                await _purchaseRequisitionDetailRepo.CreateAsync(detail);

                Console.WriteLine("DETAIL SAVED");
            }

            return createdPr;
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
            existing.updated_at = DateTime.UtcNow.AddHours(7);

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