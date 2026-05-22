using Microsoft.EntityFrameworkCore;
using trinova_erp_backend.Data;
using trinova_erp_backend.Models.Persediaan;

namespace trinova_erp_backend.Repositories.Persediaan
{
    public class MasterProductRepo
    {
        private readonly ApplicationDbContext _context;

        public MasterProductRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> InsertMasterProduct(
            MasterProduct model
        )
        {
            await _context.MasterProducts.AddAsync(model);

            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<List<MasterProduct>> GetAllMasterProduct()
        {
            var result = await _context.MasterProducts
                .Include(x => x.MasterUom)
                .Include(x => x.MasterProductCategory)
                .ToListAsync();

            return result;
        }

        public async Task<MasterProduct?> GetMasterProductById(
            int productId
        )
        {
            var result = await _context.MasterProducts
                .Include(x => x.MasterUom)
                .Include(x => x.MasterProductCategory)
                .FirstOrDefaultAsync(x =>
                    x.product_id == productId
                );

            return result;
        }

        public async Task<bool> UpdateMasterProduct(
            MasterProduct model
        )
        {
            var existingProduct = await _context.MasterProducts
                .FirstOrDefaultAsync(x =>
                    x.product_id == model.product_id
                );

            if (existingProduct == null)
            {
                return false;
            }

            existingProduct.product_name = model.product_name;
            existingProduct.product_code = model.product_code;
            existingProduct.product_type = model.product_type;
            existingProduct.category_id = model.category_id;
            existingProduct.uom_id = model.uom_id;
            existingProduct.updated_at = model.updated_at;

            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<bool> DeleteMasterProduct(
            int productId
        )
        {
            var existingProduct = await _context.MasterProducts
                .FirstOrDefaultAsync(x =>
                    x.product_id == productId
                );

            if (existingProduct == null)
            {
                return false;
            }

            _context.MasterProducts.Remove(existingProduct);

            var result = await _context.SaveChangesAsync();

            return result > 0;
        }
    }
}