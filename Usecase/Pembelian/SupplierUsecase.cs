using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface ISupplierUsecase
    {
        Task MigrateSupplierCodes();

        Task<string> GenerateSupplierCode();

        Task<Supplier?> InsertSupplier(
            Supplier model
        );

        Task<List<Supplier>>
            GetAllSupplier();

        Task<bool>
            UpdateSupplier(
                Supplier model
            );

        Task<bool>
            DeleteSupplier(
                int id
            );
    }

    public class SupplierUsecase
        : ISupplierUsecase
    {
        private readonly ISupplierRepo
            _supplierRepo;

        public SupplierUsecase(
            ISupplierRepo supplierRepo
        )
        {
            _supplierRepo =
                supplierRepo;
        }

        // ─── MIGRATE EXISTING CODES ─────────────

        public async Task MigrateSupplierCodes()
        {
            await _supplierRepo
                .MigrateSupplierCodes();
        }

        // ─── GENERATE SUPPLIER CODE ─────────────

        public async Task<string>
            GenerateSupplierCode()
        {
            return await _supplierRepo
                .GenerateSupplierCode();
        }

        // ─── INSERT ─────────────────────────────

        public async Task<Supplier?>
            InsertSupplier(
                Supplier model
            )
        {
            return await _supplierRepo
                .InsertSupplier(model);
        }

        // ─── GET ALL ────────────────────────────

        public async Task<List<Supplier>>
            GetAllSupplier()
        {
            return await _supplierRepo
                .GetAllSupplier();
        }

        // ─── UPDATE ─────────────────────────────

        public async Task<bool>
            UpdateSupplier(
                Supplier model
            )
        {
            return await _supplierRepo
                .UpdateSupplier(model);
        }

        // ─── DELETE ─────────────────────────────

        public async Task<bool>
            DeleteSupplier(
                int id
            )
        {
            return await _supplierRepo
                .DeleteSupplier(id);
        }
    }
}