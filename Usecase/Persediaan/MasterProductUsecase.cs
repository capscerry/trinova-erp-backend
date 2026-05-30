using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public interface IMasterProductUsecase
    {
        Task<string> InsertMasterProduct(MasterProduct model);

        Task<List<MasterProduct>> GetAllMasterProduct();

        Task<MasterProduct?> GetMasterProductById(int productId);

        Task<bool> UpdateMasterProduct(MasterProduct model);

        Task<bool> DeleteMasterProduct(int productId);

        Task<List<ProductDTO>> GetAllProduct();
    }

    public class MasterProductUsecase : IMasterProductUsecase
    {
        private readonly MasterProductRepo _masterProductRepo;

        public MasterProductUsecase(MasterProductRepo masterProductRepo)
        {
            _masterProductRepo = masterProductRepo;
        }

        public async Task<string> InsertMasterProduct(MasterProduct model)
        {
            model.created_at = DateTime.Now;
            model.updated_at = DateTime.Now;

            var result = await _masterProductRepo.InsertMasterProduct(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<MasterProduct>> GetAllMasterProduct()
        {
            var result = await _masterProductRepo.GetAllMasterProduct();

            return result;
        }

        public async Task<MasterProduct?> GetMasterProductById(
            int productId
        )
        {
            var result = await _masterProductRepo
                .GetMasterProductById(productId);

            return result;
        }

        public async Task<bool> UpdateMasterProduct(MasterProduct model)
        {
            model.updated_at = DateTime.Now;

            var result = await _masterProductRepo.UpdateMasterProduct(model);

            return result;
        }

        public async Task<bool> DeleteMasterProduct(int productId)
        {
            var result = await _masterProductRepo.DeleteMasterProduct(productId);

            return result;
        }

        public async Task<List<ProductDTO>> GetAllProduct()
        {
            var result = await _masterProductRepo.GetAllProduct();
            return result;
        }
    }
}