using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public interface IMasterProductCategoryUsecase
    {
        Task<string> InsertMasterProductCategory(MasterProductCategory model);

        Task<List<MasterProductCategory>> GetAllMasterProductCategory();

        Task<bool> UpdateMasterProductCategory(MasterProductCategory model);

        Task<bool> DeleteMasterProductCategory(int categoryId);
    }

    public class MasterProductCategoryUsecase : IMasterProductCategoryUsecase
    {
        private readonly MasterProductCategoryRepo _masterProductCategoryRepo;

        public MasterProductCategoryUsecase(
            MasterProductCategoryRepo masterProductCategoryRepo
        )
        {
            _masterProductCategoryRepo = masterProductCategoryRepo;
        }

        public async Task<string> InsertMasterProductCategory(
            MasterProductCategory model
        )
        {
            model.created_at = DateTime.UtcNow.AddHours(7);
            model.updated_at = DateTime.UtcNow.AddHours(7);
            model.is_active = true;

            var result = await _masterProductCategoryRepo
                .InsertMasterProductCategory(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<MasterProductCategory>> GetAllMasterProductCategory()
        {
            var result = await _masterProductCategoryRepo
                .GetAllMasterProductCategory();

            return result;
        }

        public async Task<bool> UpdateMasterProductCategory(
            MasterProductCategory model
        )
        {
            model.updated_at = DateTime.UtcNow.AddHours(7);

            var result = await _masterProductCategoryRepo
                .UpdateMasterProductCategory(model);

            return result;
        }

        public async Task<bool> DeleteMasterProductCategory(int categoryId)
        {
            var result = await _masterProductCategoryRepo
                .DeleteMasterProductCategory(categoryId);

            return result;
        }
    }
}