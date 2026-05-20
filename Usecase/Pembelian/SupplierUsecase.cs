using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface ISupplierUsecase
    {
        Task<string> InsertSupplier(Supplier model);

        Task<List<Supplier>> GetAllSupplier();

        Task<bool> UpdateSupplier(Supplier model);

        Task<bool> DeleteSupplier(int id);
    }

    public class SupplierUsecase : ISupplierUsecase
    {
        private readonly ISupplierRepo _supplierRepo;

        public SupplierUsecase(ISupplierRepo supplierRepo)
        {
            _supplierRepo = supplierRepo;
        }

        public async Task<string> InsertSupplier(Supplier model)
        {
            var result = await _supplierRepo.InsertSupplier(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<Supplier>> GetAllSupplier()
        {
            var result = await _supplierRepo.GetAllSupplier();

            return result;
        }

        public async Task<bool> UpdateSupplier(Supplier model)
        {
            var result = await _supplierRepo.UpdateSupplier(model);

            return result;
        }

        public async Task<bool> DeleteSupplier(int id)
        {
            var result = await _supplierRepo.DeleteSupplier(id);

            return result;
        }
    }
}