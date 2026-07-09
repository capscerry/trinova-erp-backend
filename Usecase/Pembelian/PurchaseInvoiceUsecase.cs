using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseInvoiceUsecase
    {
        Task<string> GetNextInvoiceNumber();

        Task<int> InsertPurchaseInvoice(
            PurchaseInvoice model
        );

        Task<List<PurchaseInvoice>>
            GetAllPurchaseInvoice();

        Task<List<PurchaseInvoice>> GetUnpaidInvoicesBySupplier(
            int supplierId
        );

        Task<bool> UpdatePurchaseInvoice(
            PurchaseInvoice model
        );

        Task<bool> DeletePurchaseInvoice(
            int id
        );

        /// <summary>
        /// Recalculates the outstanding_amount for one invoice and updates
        /// its status to 'Paid' or 'Unpaid'. Cancelled invoices are skipped.
        /// </summary>
        Task SyncInvoiceStatus(int purchaseInvoiceId);

        /// <summary>
        /// Recalculates outstanding_amount for every non-Cancelled invoice
        /// and bulk-updates their status. Used for the one-time backfill.
        /// </summary>
        Task SyncAllInvoiceStatuses();
    }

    public class PurchaseInvoiceUsecase
        : IPurchaseInvoiceUsecase
    {
        private readonly IPurchaseInvoiceRepo
            _purchaseInvoiceRepo;

        public PurchaseInvoiceUsecase(
            IPurchaseInvoiceRepo purchaseInvoiceRepo
        )
        {
            _purchaseInvoiceRepo = purchaseInvoiceRepo;
        }

        public async Task<string> GetNextInvoiceNumber()
        {
            return await _purchaseInvoiceRepo
                .GenerateInvoiceNumber();
        }

        public async Task<int>
            InsertPurchaseInvoice(
                PurchaseInvoice model
            )
        {
            model.created_at   = DateTime.Now;
            model.invoice_date = DateTime.Now;
            model.status       = "Unpaid";

            model.invoice_number =
                await _purchaseInvoiceRepo
                    .GenerateInvoiceNumber();

            bool isExist =
                await _purchaseInvoiceRepo
                    .IsInvoiceExist(model.goods_receipt_id);

            if (isExist)
            {
                throw new Exception(
                    "Invoice already exists for this Goods Receipt"
                );
            }

            return await _purchaseInvoiceRepo
                .InsertPurchaseInvoice(model);
        }

        public async Task<List<PurchaseInvoice>>
            GetAllPurchaseInvoice()
        {
            return await _purchaseInvoiceRepo
                .GetAllPurchaseInvoice();
        }

        public async Task<List<PurchaseInvoice>> GetUnpaidInvoicesBySupplier(
            int supplierId
        )
        {
            return await _purchaseInvoiceRepo
                .GetUnpaidInvoicesBySupplier(supplierId);
        }

        public async Task<bool>
            UpdatePurchaseInvoice(
                PurchaseInvoice model
            )
        {
            var existing =
                await _purchaseInvoiceRepo
                    .GetPurchaseInvoiceById(
                        model.purchase_invoice_id
                    );

            if (existing == null)
            {
                throw new KeyNotFoundException(
                    $"Purchase Invoice with id {model.purchase_invoice_id} not found"
                );
            }

            if (existing.status == "Cancelled")
            {
                throw new InvalidOperationException(
                    "Cancelled invoices cannot be updated"
                );
            }

            if (existing.status == model.status)
            {
                return true;
            }

            return await _purchaseInvoiceRepo
                .UpdatePurchaseInvoice(model);
        }

        public async Task<bool>
            DeletePurchaseInvoice(
                int id
            )
        {
            var existing =
                await _purchaseInvoiceRepo
                    .GetPurchaseInvoiceById(id);

            if (existing == null)
            {
                return false;
            }

            if (
                existing.status == "Paid"
                || existing.status == "Cancelled"
            )
            {
                return false;
            }

            return await _purchaseInvoiceRepo
                .DeletePurchaseInvoice(id);
        }

        public async Task SyncInvoiceStatus(int purchaseInvoiceId)
        {
            await _purchaseInvoiceRepo
                .SyncInvoiceStatus(purchaseInvoiceId);
        }

        public async Task SyncAllInvoiceStatuses()
        {
            await _purchaseInvoiceRepo
                .SyncAllInvoiceStatuses();
        }
    }
}
