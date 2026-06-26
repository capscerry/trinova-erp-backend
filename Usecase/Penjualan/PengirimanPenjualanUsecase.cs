using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface IPengirimanPenjualanUsecase
    {
        Task<List<ShippingDTO>> GetShippingCategory();
        Task<List<DeliveryOrderHeaderDTO>> GetDoHeader();
        Task<List<DeliveryOrderDetailDTO>> GetDoDetail(int deliveryOrderId);
        Task InsertDeliveryOrder(PengirimanPenjualan model);

    }
    public class PengirimanPenjualanUsecase : IPengirimanPenjualanUsecase
    {
        private readonly IPengirimanPenjualanRepo _pengirimanRepo;
        private readonly string _connectionString;

        public PengirimanPenjualanUsecase(IPengirimanPenjualanRepo pengirimanRepo,IOptions<DatabaseConnection> options)
        {
            _pengirimanRepo = pengirimanRepo;
            _connectionString = options.Value.SQLServer;
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

        public async Task<List<DeliveryOrderDetailDTO>> GetDoDetail(int deliveryOrderId)
        {
            if (deliveryOrderId <= 0)
                throw new Exception("Id pengiriman tidak valid.");

            var result = await _pengirimanRepo.GetDoDetail(deliveryOrderId);
            return result;
        }

        public async Task InsertDeliveryOrder(PengirimanPenjualan model)
        {
            using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                var header = new DeliveryOrderHeaderDTO
                {
                    CustomerId = model.Header.CustomerId,
                    DoNumber = model.Header.DoNumber,
                    SoId = model.Header.SoId,
                    PoNumber = model.Header.PoNumber,
                    DoDate = model.Header.DoDate,
                    DeliveryCategoryId = model.Header.DeliveryCategoryId,
                    Address = model.Header.Address,
                    Notes = model.Header.Notes
                };

                int doId = await _pengirimanRepo.InsertDeliveryOrderHeader(
                    header,
                    connection,
                    transaction);

                foreach (var detail in model.Detail)
                {
                    var detailDTO = new DeliveryOrderDetailDTO
                    {
                        DoId = doId, // sesuaikan nama property DTO
                        ProductId = detail.ProductId,
                        QtyDikirim = detail.QtyDikirim,
                        QtyDipesan = detail.QtyDipesan
                    };

                    await _pengirimanRepo.InsertDeliveryOrderDetail(
                        detailDTO,
                        connection,
                        transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
