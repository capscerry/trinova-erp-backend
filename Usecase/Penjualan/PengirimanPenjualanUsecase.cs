using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Penjualan;
using trinova_erp_backend.Repositories.Persediaan;

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
        private readonly InventoryStockRepo _inventoryStockRepo;
        private readonly StockTransactionRepo _stockTransactionRepo;
        private readonly StockMovementRepo _stockMovementRepo;
        private readonly string _connectionString;
        private readonly trinova_erp_backend.Services.IActivityLogService _activityLogService;

        public PengirimanPenjualanUsecase(
            IPengirimanPenjualanRepo pengirimanRepo,
            InventoryStockRepo inventoryStockRepo,
            StockTransactionRepo stockTransactionRepo,
            StockMovementRepo stockMovementRepo,
            IOptions<DatabaseConnection> options,
            trinova_erp_backend.Services.IActivityLogService activityLogService)
        {
            _pengirimanRepo = pengirimanRepo;
            _inventoryStockRepo = inventoryStockRepo;
            _stockTransactionRepo = stockTransactionRepo;
            _stockMovementRepo = stockMovementRepo;
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

                // Tidak ada langkah "konfirmasi" terpisah di alur Delivery
                // Order — dokumen DO itu sendiri ADALAH bukti barang keluar
                // gudang, jadi stok langsung dikurangi di sini, satu
                // transaksi dengan pembuatan dokumennya.
                var isLinkedToSalesOrder = header.SoId.HasValue && header.SoId.Value > 0;

                foreach (var detail in model.Detail)
                {
                    var warehouseId = await ResolveWarehouseIdAsync(
                        detail.WarehouseId,
                        header.SoId,
                        detail.ProductId,
                        connection,
                        transaction);

                    if (warehouseId == null || warehouseId <= 0)
                        throw new InvalidOperationException(
                            $"Produk (id {detail.ProductId}) belum memiliki gudang, delivery order tidak bisa dibuat.");

                    var detailDTO = new DeliveryOrderDetailDTO
                    {
                        DoId = doId, // sesuaikan nama property DTO
                        ProductId = detail.ProductId,
                        QtyDikirim = detail.QtyDikirim,
                        QtyDipesan = detail.QtyDipesan,
                        WarehouseId = warehouseId
                    };

                    await _pengirimanRepo.InsertDeliveryOrderDetail(
                        detailDTO,
                        connection,
                        transaction);

                    if (detail.QtyDikirim <= 0)
                        continue;

                    if (isLinkedToSalesOrder)
                    {
                        await _inventoryStockRepo.DeductReservedAsync(
                            connection,
                            transaction,
                            detail.ProductId,
                            warehouseId.Value,
                            detail.QtyDikirim);
                    }
                    else
                    {
                        await _inventoryStockRepo.DeductAvailableAsync(
                            connection,
                            transaction,
                            detail.ProductId,
                            warehouseId.Value,
                            detail.QtyDikirim);
                    }

                    await _stockTransactionRepo.CreateAsync(
                        new StockTransaction
                        {
                            product_id = detail.ProductId,
                            warehouse_id = warehouseId.Value,
                            transaction_type = "OUT",
                            quantity = detail.QtyDikirim,
                            reference_no = header.DoNumber,
                            reference_module = "DELIVERY_ORDER",
                            reference_id = doId,
                            remarks = "Stok keluar saat Delivery Order dibuat",
                            created_at = DateTime.Now
                        },
                        connection,
                        transaction);

                    await _stockMovementRepo.InsertAsync(
                        new StockMovement
                        {
                            product_id = detail.ProductId,
                            movement_type = "OUTBOUND",
                            quantity = detail.QtyDikirim,
                            reference_number = header.DoNumber,
                            notes = "Delivery Order created",
                            movement_date = DateTime.Now,
                            created_at = DateTime.Now,
                            source_warehouse_id = warehouseId.Value
                        },
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
                    var warehouseId = await ResolveWarehouseIdAsync(
                        detail.WarehouseId,
                        header.SoId,
                        detail.ProductId,
                        connection,
                        transaction);

                    var detailDTO = new DeliveryOrderDetailDTO
                    {
                        DoId = id,
                        ProductId = detail.ProductId,
                        QtyDikirim = detail.QtyDikirim,
                        QtyDipesan = detail.QtyDipesan,
                        WarehouseId = warehouseId
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

        // Uses the warehouse chosen on the DO line as-is. Only falls back to
        // the linked Sales Order's warehouse for that product when the
        // client didn't send one (safety net for older/manual clients).
        private async Task<int?> ResolveWarehouseIdAsync(
            int? warehouseId,
            int? soId,
            int productId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            if (warehouseId.HasValue && warehouseId.Value > 0)
                return warehouseId;

            if (!soId.HasValue || soId.Value <= 0)
                return null;

            return await _pengirimanRepo.GetSalesOrderLineWarehouseAsync(
                soId.Value,
                productId,
                connection,
                transaction);
        }

    }

}
