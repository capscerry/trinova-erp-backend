using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public interface IMasterWarehouseUsecase
    {
        Task<string> InsertMasterWarehouse(MasterWarehouse model);

        Task<List<MasterWarehouse>> GetAllMasterWarehouse();

        Task<bool> UpdateMasterWarehouse(MasterWarehouse model);

        Task<bool> DeleteMasterWarehouse(int warehouseId);
    }

    public class MasterWarehouseUsecase : IMasterWarehouseUsecase
    {
        private readonly MasterWarehouseRepo _masterWarehouseRepo;

        public MasterWarehouseUsecase(
            MasterWarehouseRepo masterWarehouseRepo
        )
        {
            _masterWarehouseRepo = masterWarehouseRepo;
        }

        public async Task<string> InsertMasterWarehouse(
            MasterWarehouse model
        )
        {
            model.created_at = DateTime.UtcNow.AddHours(7);
            model.updated_at = DateTime.UtcNow.AddHours(7);
            model.is_active = true;

            var result = await _masterWarehouseRepo
                .InsertMasterWarehouse(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<MasterWarehouse>> GetAllMasterWarehouse()
        {
            var result = await _masterWarehouseRepo
                .GetAllMasterWarehouse();

            return result;
        }

        public async Task<bool> UpdateMasterWarehouse(
            MasterWarehouse model
        )
        {
            model.updated_at = DateTime.UtcNow.AddHours(7);

            var result = await _masterWarehouseRepo
                .UpdateMasterWarehouse(model);

            return result;
        }

        public async Task<bool> DeleteMasterWarehouse(int warehouseId)
        {
            var result = await _masterWarehouseRepo
                .DeleteMasterWarehouse(warehouseId);

            return result;
        }
    }
}