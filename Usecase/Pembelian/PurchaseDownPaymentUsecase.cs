using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseDownPaymentUsecase
    {
        Task<string> GetNextDPNumber();

        Task<int> InsertPurchaseDownPayment(
            PurchaseDownPayment model
        );

        Task<List<PurchaseDownPayment>>
            GetAllPurchaseDownPayment();

        Task<bool> DeletePurchaseDownPayment(int id);
    }

    public class PurchaseDownPaymentUsecase
        : IPurchaseDownPaymentUsecase
    {
        private readonly
            IPurchaseDownPaymentRepo
            _purchaseDownPaymentRepo;

        private readonly
            ISupplierRepo
            _supplierRepo;

        public PurchaseDownPaymentUsecase(
            IPurchaseDownPaymentRepo
                purchaseDownPaymentRepo,

            ISupplierRepo
                supplierRepo
        )
        {
            _purchaseDownPaymentRepo =
                purchaseDownPaymentRepo;

            _supplierRepo =
                supplierRepo;
        }

        public async Task<string> GetNextDPNumber()
        {
            return await _purchaseDownPaymentRepo
                .GenerateDPNumber();
        }

        public async Task<int>
            InsertPurchaseDownPayment(
                PurchaseDownPayment model
            )
        {
            var supplier =
                await _supplierRepo
                    .GetSupplierById(
                        model.supplier_id
                    );

            if (supplier == null)
            {
                return 0;
            }

            if (supplier.status != "Active")
            {
                return 0;
            }

            // Duplicate-submission guard: reject if a DP already exists for this PO
            bool isExist =
                await _purchaseDownPaymentRepo
                    .IsDownPaymentExist(model.purchase_order_id);

            if (isExist)
            {
                throw new InvalidOperationException(
                    "Purchase Down Payment already exists for this Purchase Order."
                );
            }

            model.created_at =
                DateTime.Now;

            model.status = "Paid";

            model.dp_number =
                await _purchaseDownPaymentRepo
                    .GenerateDPNumber();

            var result =
                await _purchaseDownPaymentRepo
                    .InsertPurchaseDownPayment(
                        model
                    );

            return result;
        }

        public async Task<List<PurchaseDownPayment>>
            GetAllPurchaseDownPayment()
        {
            return await
                _purchaseDownPaymentRepo
                    .GetAllPurchaseDownPayment();
        }

        public async Task<bool> DeletePurchaseDownPayment(int id)
        {
            return await
                _purchaseDownPaymentRepo
                    .DeletePurchaseDownPayment(id);
        }
    }
}