using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Persediaan
{
    public class MasterProductSubcategoryUsecase
    {
        private readonly MasterProductSubcategoryRepo _repository;

        public MasterProductSubcategoryUsecase(
            MasterProductSubcategoryRepo repository
        )
        {
            _repository = repository;
        }

        public async Task<List<ProductSubcategory>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<List<ProductSubcategory>> GetByCategoryAsync(
            int categoryId
        )
        {
            return await _repository.GetByCategoryAsync(categoryId);
        }

        public async Task<ProductSubcategory?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ProductSubcategory> CreateAsync(
            ProductSubcategory subcategory
        )
        {
            var existingSubcategory =
                await _repository.GetByNameAsync(
                    subcategory.name
                );

            if (existingSubcategory != null)
            {
                throw new Exception(
                    "Subcategory sudah terdaftar"
                );
            }

            subcategory.code =
                await _repository
                    .GenerateNextCodeAsync();

            subcategory.created_at =
                DateTime.UtcNow;

            subcategory.updated_at =
                DateTime.UtcNow;

            subcategory.is_active = true;

            return await _repository.CreateAsync(
                subcategory
            );
        }

        public async Task<ProductSubcategory?> UpdateAsync(
            int id,
            ProductSubcategory model
        )
        {
            var subcategory = await _repository.GetByIdAsync(id);

            if (subcategory == null)
            {
                return null;
            }

            subcategory.category_id = model.category_id;
            subcategory.code = model.code;
            subcategory.name = model.name;
            subcategory.updated_at = DateTime.UtcNow;

            return await _repository.UpdateAsync(subcategory);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var subcategory = await _repository.GetByIdAsync(id);

            if (subcategory == null)
            {
                return false;
            }

            return await _repository.DeleteAsync(subcategory);
        }
    }
}