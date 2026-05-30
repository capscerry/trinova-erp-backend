using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface ISupplierCategoryUsecase
    {
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

        public async Task<string> InsertSupplierCategory(SupplierCategory model)
        {
            var result = await _supplierCategoryRepo.InsertSupplierCategory(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<SupplierCategory>> GetAllSupplierCategory()
        {
            var result = await _supplierCategoryRepo.GetAllSupplierCategory();

            return result;
        }

        public async Task<bool> UpdateSupplierCategory(SupplierCategory model)
        {
            var result = await _supplierCategoryRepo.UpdateSupplierCategory(model);

            return result;
        }

        public async Task<bool>
        DeleteSupplierCategory(int id)
    {
        bool isUsed =
            await _supplierCategoryRepo
                .IsCategoryUsed(id);

        if (isUsed)
        {
            throw new Exception(
                "Category masih digunakan oleh supplier"
            );
        }

        return await _supplierCategoryRepo
            .DeleteSupplierCategory(id);
    }
    }
}