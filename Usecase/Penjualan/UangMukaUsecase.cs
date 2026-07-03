using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface IUangMukaUsecase
    {
        Task<bool> InsertUangMuka(UangMuka model);
        Task<bool> UpdateUangMuka(int id, UangMuka model);
        Task<IEnumerable<UangMuka>> GetAllUangMuka();
        Task<UangMuka?> GetUangMukaById(int id);
    }

    public class UangMukaUsecase : IUangMukaUsecase
    {
        private readonly IUangMukaRepositories _uangMuka;
        private readonly trinova_erp_backend.Services.IActivityLogService _activityLogService;

        public UangMukaUsecase(
            IUangMukaRepositories uangMuka,
            trinova_erp_backend.Services.IActivityLogService activityLogService)
        {
            _uangMuka = uangMuka;
            _activityLogService = activityLogService;
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

            if (string.IsNullOrWhiteSpace(model.Status) || model.Status == "Received")
                model.Status = "Issued";

            var result = await _uangMuka.InsertUangMuka(model);

            if (result)
            {
                await _activityLogService.LogSalesAsync(
                    "down_payment_created",
                    $"Sales Down Payment {model.NoFaktur} created",
                    $"Down payment recorded for {model.CustomerName ?? "customer"}.",
                    "uang_muka",
                    null,
                    model.NoFaktur);
            }

            return result;
        }

        public async Task<IEnumerable<UangMuka>> GetAllUangMuka()
        {
            return await _uangMuka.GetAllUangMuka();
        }

        public async Task<bool> UpdateUangMuka(int id, UangMuka model)
        {
            if (model == null)
                throw new Exception("Data uang muka tidak boleh kosong");

            if (id <= 0)
                throw new Exception("Id uang muka tidak valid");

            if (string.IsNullOrWhiteSpace(model.NoFaktur))
                throw new Exception("No faktur wajib diisi");

            if (model.CustomerId <= 0)
                throw new Exception("Customer wajib dipilih");

            if (model.NominalUangMuka <= 0)
                throw new Exception("Nominal uang muka harus lebih dari 0");

            model.Id = id;

            if (string.IsNullOrWhiteSpace(model.UpdatedBy))
                model.UpdatedBy = "SYSTEM";

            if (string.IsNullOrWhiteSpace(model.Status))
                model.Status = "Issued";

            var result = await _uangMuka.UpdateUangMuka(model);

            if (result)
            {
                await _activityLogService.LogSalesAsync(
                    "down_payment_updated",
                    $"Sales Down Payment {model.NoFaktur} updated",
                    $"Down payment updated for {model.CustomerName ?? "customer"}.",
                    "uang_muka",
                    id,
                    model.NoFaktur);
            }

            return result;
        }

        public async Task<UangMuka?> GetUangMukaById(int id)
        {
            if (id <= 0)
                throw new Exception("Id uang muka tidak valid");

            return await _uangMuka.GetUangMukaById(id);
        }
    }
}
