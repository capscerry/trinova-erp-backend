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
        Task UpdateDeliveryOrder(int id, PengirimanPenjualan model);

    }
    public class PengirimanPenjualanUsecase : IPengirimanPenjualanUsecase
    {
        private readonly IPengirimanPenjualanRepo _pengirimanRepo;
        private readonly string _connectionString;
        private readonly trinova_erp_backend.Services.IActivityLogService _activityLogService;

        public PengirimanPenjualanUsecase(
            IPengirimanPenjualanRepo pengirimanRepo,
            IOptions<DatabaseConnection> options,
            trinova_erp_backend.Services.IActivityLogService activityLogService)
        {
            _pengirimanRepo = pengirimanRepo;
            _connectionString = options.Value.SQLServer;
            _activityLogService = activityLogService;
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

                await _activityLogService.LogSalesAsync(
                    "delivery_order_created",
                    $"Delivery Order {header.DoNumber} created",
                    $"Delivery order created for {model.Header.CustomerName ?? "customer"}.",
                    "delivery_order_header",
                    doId,
                    header.DoNumber);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateDeliveryOrder(int id, PengirimanPenjualan model)
        {
            if (id <= 0)
                throw new Exception("Id pengiriman tidak valid.");

            if (model?.Header == null)
                throw new Exception("Header pengiriman wajib diisi.");

            if (model.Header.CustomerId <= 0)
                throw new Exception("Customer wajib dipilih.");

            if (model.Header.DeliveryCategoryId <= 0)
                throw new Exception("Tipe pengiriman wajib dipilih.");

            if (string.IsNullOrWhiteSpace(model.Header.DoNumber))
                throw new Exception("No surat jalan wajib diisi.");

            if (model.Detail == null || model.Detail.Count == 0)
                throw new Exception("Detail barang wajib diisi.");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var header = new DeliveryOrderHeaderDTO
                {
                    Id = id,
                    CustomerId = model.Header.CustomerId,
                    DoNumber = model.Header.DoNumber,
                    SoId = model.Header.SoId,
                    PoNumber = model.Header.PoNumber,
                    DoDate = model.Header.DoDate,
                    DeliveryCategoryId = model.Header.DeliveryCategoryId,
                    Address = model.Header.Address,
                    Notes = model.Header.Notes
                };

                await _pengirimanRepo.UpdateDeliveryOrderHeader(header, connection, transaction);
                await _pengirimanRepo.DeleteDeliveryOrderDetail(id, connection, transaction);

                foreach (var detail in model.Detail)
                {
                    var detailDTO = new DeliveryOrderDetailDTO
                    {
                        DoId = id,
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

                await _activityLogService.LogSalesAsync(
                    "delivery_order_updated",
                    $"Delivery Order {header.DoNumber} updated",
                    $"Delivery order data was updated.",
                    "delivery_order_header",
                    id,
                    header.DoNumber);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
