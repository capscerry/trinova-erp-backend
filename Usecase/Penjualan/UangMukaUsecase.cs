using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface IUangMukaUsecase
    {
        Task<bool> InsertUangMuka(UangMuka model);
        Task<IEnumerable<UangMuka>> GetAllUangMuka();
        Task<UangMuka?> GetUangMukaById(int id);
    }

    public class UangMukaUsecase : IUangMukaUsecase
    {
        private readonly IUangMukaRepositories _uangMuka;

        public UangMukaUsecase(IUangMukaRepositories uangMuka)
        {
            _uangMuka = uangMuka;
        }

        public async Task<bool> InsertUangMuka(UangMuka model)
        {
            if (model == null)
                throw new Exception("Data uang muka tidak boleh kosong");

            if (string.IsNullOrWhiteSpace(model.NoFaktur))
                throw new Exception("No faktur wajib diisi");

            if (model.CustomerId <= 0)
                throw new Exception("Customer wajib dipilih");

            if (model.NominalUangMuka <= 0)
                throw new Exception("Nominal uang muka harus lebih dari 0");

            if (string.IsNullOrWhiteSpace(model.CreatedBy))
                model.CreatedBy = "SYSTEM";

            return await _uangMuka.InsertUangMuka(model);
        }

        public async Task<IEnumerable<UangMuka>> GetAllUangMuka()
        {
            return await _uangMuka.GetAllUangMuka();
        }

        public async Task<UangMuka?> GetUangMukaById(int id)
        {
            if (id <= 0)
                throw new Exception("Id uang muka tidak valid");

            return await _uangMuka.GetUangMukaById(id);
        }
    }
}