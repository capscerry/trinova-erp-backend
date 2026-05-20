using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public interface IMasterUomUsecase
    {
        Task<List<MasterUom>> GetAllMasterUom();
    }

    public class MasterUomUsecase : IMasterUomUsecase
    {
        private readonly IMasterUomRepo _masterUomRepo;

        public MasterUomUsecase(IMasterUomRepo masterUomRepo)
        {
            _masterUomRepo = masterUomRepo;
        }

        public async Task<List<MasterUom>> GetAllMasterUom()
        {
            return await _masterUomRepo.GetAllMasterUom();
        }
    }
}