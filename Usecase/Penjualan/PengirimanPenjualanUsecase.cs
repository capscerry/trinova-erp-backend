using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface IPengirimanPenjualanUsecase
    {
        Task<List<ShippingDTO>> GetShippingCategory();
        Task<List<DeliveryOrderHeaderDTO>> GetDoHeader();
    }
    public class PengirimanPenjualanUsecase : IPengirimanPenjualanUsecase
    {
        private readonly IPengirimanPenjualanRepo _pengirimanRepo;

        public PengirimanPenjualanUsecase(IPengirimanPenjualanRepo pengirimanRepo)
        {
            _pengirimanRepo = pengirimanRepo;
        }

        public async Task<List<ShippingDTO>> GetShippingCategory()
        {
            var result = await _pengirimanRepo.GetShippingCategory();
            return result;
        }

        public async Task<List<DeliveryOrderHeaderDTO>> GetDoHeader()
        {
            var result = await _pengirimanRepo.GetDoHeader();
            return result;
        }
    }
}
