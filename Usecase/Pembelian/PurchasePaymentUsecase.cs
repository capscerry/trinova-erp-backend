using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchasePaymentUsecase
    {
        Task<string> GetNextPaymentNumber();

        Task<int> InsertPurchasePayment(
            PurchasePayment model
        );

        Task<List<PurchasePayment>>
            GetAllPurchasePayment();

        Task<bool> DeletePurchasePayment(
            int id
        );
    }

    public class PurchasePaymentUsecase
        : IPurchasePaymentUsecase
    {
        private readonly
            IPurchasePaymentRepo
            _purchasePaymentRepo;

        public PurchasePaymentUsecase(
            IPurchasePaymentRepo
                purchasePaymentRepo
        )
        {
            _purchasePaymentRepo =
                purchasePaymentRepo;
        }

        public async Task<string> GetNextPaymentNumber()
        {
            return await _purchasePaymentRepo
                .GeneratePaymentNumber();
        }

        public async Task<int>
            InsertPurchasePayment(
                PurchasePayment model
            )
        {
            model.payment_number =
                await _purchasePaymentRepo
                    .GeneratePaymentNumber();

            return await
                _purchasePaymentRepo
                    .InsertPurchasePayment(
                        model
                    );
        }

        public async Task<List<PurchasePayment>>
            GetAllPurchasePayment()
        {
            return await
                _purchasePaymentRepo
                    .GetAllPurchasePayment();
        }

        public async Task<bool>
            DeletePurchasePayment(
                int id
            )
        {
            return await
                _purchasePaymentRepo
                    .DeletePurchasePayment(
                        id
                    );
        }
    }
}