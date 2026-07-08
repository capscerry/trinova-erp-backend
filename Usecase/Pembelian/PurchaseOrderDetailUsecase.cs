using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories.Pembelian;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseOrderDetailUsecase
    {
        Task<string> InsertPurchaseOrderDetail(PurchaseOrderDetail model);

        Task<List<PurchaseOrderDetail>> GetAllPurchaseOrderDetail();

        Task<List<PurchaseOrderDetail>> GetDetailsByPurchaseOrderId(int purchaseOrderId);

        Task<bool> UpdatePurchaseOrderDetail(PurchaseOrderDetail model);

        Task<bool> DeletePurchaseOrderDetail(int id);
    }

    public class PurchaseOrderDetailUsecase : IPurchaseOrderDetailUsecase
    {
        private readonly IPurchaseOrderDetailRepo _purchaseOrderDetailRepo;

        public PurchaseOrderDetailUsecase(
            IPurchaseOrderDetailRepo purchaseOrderDetailRepo
        )
        {
            _purchaseOrderDetailRepo = purchaseOrderDetailRepo;
        }

        public async Task<string> InsertPurchaseOrderDetail(
            PurchaseOrderDetail model
        )
        {
            decimal baseAmount = (model.quantity * (model.price ?? 0m));
            decimal taxRate    = (model.tax_percentage ?? 0m) / 100m;
            model.tax_amount   = Math.Round(baseAmount * taxRate, 2);
            model.subtotal     = baseAmount + model.tax_amount;

            var result = await _purchaseOrderDetailRepo
                .InsertPurchaseOrderDetail(model);

            return result ? "Insert Successfully" : "Insert Failed";
        }

        public async Task<List<PurchaseOrderDetail>>
            GetAllPurchaseOrderDetail()
        {
            var result = await _purchaseOrderDetailRepo
                .GetAllPurchaseOrderDetail();

            return result;
        }

        public async Task<List<PurchaseOrderDetail>>
            GetDetailsByPurchaseOrderId(int purchaseOrderId)
        {
            return await _purchaseOrderDetailRepo
                .GetDetailsByPurchaseOrderId(purchaseOrderId);
        }

        public async Task<bool> UpdatePurchaseOrderDetail(
            PurchaseOrderDetail model
        )
        {
            decimal baseAmount = (model.quantity * (model.price ?? 0m));
            decimal taxRate    = (model.tax_percentage ?? 0m) / 100m;
            model.tax_amount   = Math.Round(baseAmount * taxRate, 2);
            model.subtotal     = baseAmount + model.tax_amount;

            var result = await _purchaseOrderDetailRepo
                .UpdatePurchaseOrderDetail(model);

            return result;
        }

        public async Task<bool> DeletePurchaseOrderDetail(int id)
        {
            var result = await _purchaseOrderDetailRepo
                .DeletePurchaseOrderDetail(id);

            return result;
        }
    }
}