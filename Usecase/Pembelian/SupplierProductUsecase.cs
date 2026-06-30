using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface ISupplierProductUsecase
    {
        Task<bool> InsertSupplierProduct(
            SupplierProduct model
        );

        Task<List<SupplierProduct>>
            GetAllSupplierProduct();

        Task<bool> BulkInsertSupplierProduct(
            int supplierId,
            List<SupplierProductImport> models
        );

        Task<bool> RestoreStock(
            int productId,
            int supplierId,
            int quantity
        );
    }

    public class SupplierProductUsecase
        : ISupplierProductUsecase
    {
        private readonly ISupplierProductRepo
            _supplierProductRepo;

        public SupplierProductUsecase(
            ISupplierProductRepo supplierProductRepo
        )
        {
            _supplierProductRepo =
                supplierProductRepo;
        }

        public async Task<bool>
            InsertSupplierProduct(
                SupplierProduct model
            )
        {
            model.created_at =
                DateTime.Now;

            return await _supplierProductRepo
                .InsertSupplierProduct(model);
        }

        public async Task<List<SupplierProduct>>
            GetAllSupplierProduct()
        {
            return await _supplierProductRepo
                .GetAllSupplierProduct();
        }

        public async Task<bool>
            BulkInsertSupplierProduct(
                int supplierId,
                List<SupplierProductImport> models
            )
        {
            var supplierProducts =
                new List<SupplierProduct>();

            foreach (var item in models)
            {
                supplierProducts.Add(
                    new SupplierProduct
                    {
                        supplier_id =
                            supplierId,

                        product_id =
                            item.product_id,

                        supplier_price =
                            item.supplier_price,

                        available_stock =
                            item.available_stock,

                        lead_time_days =
                            item.lead_time_days,

                        is_available =
                            true,

                        created_at =
                            DateTime.Now
                    }
                );
            }

            return await _supplierProductRepo
                .BulkInsertSupplierProduct(
                    supplierProducts
                );
        }

        public async Task<bool> RestoreStock(
            int productId,
            int supplierId,
            int quantity
        )
        {
            return await _supplierProductRepo
                .RestoreStock(productId, supplierId, quantity);
        }
    }
}