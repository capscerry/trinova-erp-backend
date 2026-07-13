using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface ISupplierCategoryUsecase
    {
        Task MigrateCategoryCodes();

        Task<string> GenerateCategoryCode();

        Task<string> InsertSupplierCategory(SupplierCategory model);

        Task<List<SupplierCategory>> GetAllSupplierCategory();

        Task<bool> UpdateSupplierCategory(SupplierCategory model);

        Task<bool> DeleteSupplierCategory(int id);
    }

    public class SupplierCategoryUsecase : ISupplierCategoryUsecase
    {
        private readonly ISupplierCategoryRepo _supplierCategoryRepo;

        public SupplierCategoryUsecase(ISupplierCategoryRepo supplierCategoryRepo)
        {
            _supplierCategoryRepo = supplierCategoryRepo;
        }

        public async Task MigrateCategoryCodes()
        {
            await _supplierCategoryRepo.MigrateCategoryCodes();
        }

        public async Task<string> GenerateCategoryCode()
        {
            return await _supplierCategoryRepo.GenerateCategoryCode();
        }

        public async Task<string> InsertSupplierCategory(SupplierCategory model)
        {
            var result = await _supplierCategoryRepo.InsertSupplierCategory(model);
            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<SupplierCategory>> GetAllSupplierCategory()
        {
            return await _supplierCategoryRepo.GetAllSupplierCategory();
        }

        public async Task<bool> UpdateSupplierCategory(SupplierCategory model)
        {
            return await _supplierCategoryRepo.UpdateSupplierCategory(model);
        }

        public async Task<bool> DeleteSupplierCategory(int id)
        {
            bool isUsed = await _supplierCategoryRepo.IsCategoryUsed(id);

            if (isUsed)
                throw new Exception("Category masih digunakan oleh supplier");

            return await _supplierCategoryRepo.DeleteSupplierCategory(id);
        }
    }
}
