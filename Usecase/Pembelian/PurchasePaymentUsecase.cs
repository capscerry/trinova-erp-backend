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

        Task<bool> UpdatePurchasePayment(
            int id,
            PurchasePayment model
        );

        Task<bool> DeletePurchasePayment(
            int id
        );
    }

    public class PurchasePaymentUsecase
        : IPurchasePaymentUsecase
    {
        private readonly IPurchasePaymentRepo _purchasePaymentRepo;
        private readonly IPurchaseInvoiceRepo _purchaseInvoiceRepo;

        public PurchasePaymentUsecase(
            IPurchasePaymentRepo purchasePaymentRepo,
            IPurchaseInvoiceRepo purchaseInvoiceRepo
        )
        {
            _purchasePaymentRepo = purchasePaymentRepo;
            _purchaseInvoiceRepo = purchaseInvoiceRepo;
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

            bool isDuplicate = await _purchasePaymentRepo
                .IsDuplicatePayment(
                    model.purchase_invoice_id,
                    model.payment_date,
                    model.amount
                );

            if (isDuplicate)
                throw new InvalidOperationException(
                    "A payment with the same invoice, date, and amount already exists"
                );

            int paymentId = await _purchasePaymentRepo
                .InsertPurchasePayment(model);

            // Auto-sync the invoice status after recording the payment
            await _purchaseInvoiceRepo
                .SyncInvoiceStatus(model.purchase_invoice_id);

            return paymentId;
        }

        public async Task<List<PurchasePayment>>
            GetAllPurchasePayment()
        {
            return await _purchasePaymentRepo
                .GetAllPurchasePayment();
        }

        public async Task<bool>
            DeletePurchasePayment(
                int id
            )
        {
            // Resolve the invoice before deleting so we can sync after
            int? invoiceId = await _purchasePaymentRepo
                .GetInvoiceIdByPaymentId(id);

            bool result = await _purchasePaymentRepo
                .DeletePurchasePayment(id);

            if (result && invoiceId.HasValue)
                await _purchaseInvoiceRepo
                    .SyncInvoiceStatus(invoiceId.Value);

            return result;
        }

        public async Task<bool>
            UpdatePurchasePayment(
                int id,
                PurchasePayment model
            )
        {
            // Resolve the invoice before updating (amount may change)
            int? invoiceId = await _purchasePaymentRepo
                .GetInvoiceIdByPaymentId(id);

            bool isDuplicate = await _purchasePaymentRepo
                .IsDuplicatePayment(
                    model.purchase_invoice_id,
                    model.payment_date,
                    model.amount,
                    excludePaymentId: id
                );

            if (isDuplicate)
                throw new InvalidOperationException(
                    "A payment with the same invoice, date, and amount already exists"
                );

            bool result = await _purchasePaymentRepo
                .UpdatePurchasePayment(id, model);

            if (result && invoiceId.HasValue)
                await _purchaseInvoiceRepo
                    .SyncInvoiceStatus(invoiceId.Value);

            return result;
        }
    }
}