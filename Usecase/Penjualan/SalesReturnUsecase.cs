using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Penjualan;
using trinova_erp_backend.Repositories.Persediaan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface ISalesReturnUsecase
    {
        Task<List<SalesReturnHeaderDTO>> GetAllAsync();
        Task<SalesReturn?> GetDetailAsync(int id);
        Task<Dictionary<int, decimal>> GetReturnableQtyAsync(int deliveryOrderId);
        Task<int> InsertSalesReturn(SalesReturn model);
    }

    public class SalesReturnUsecase : ISalesReturnUsecase
    {
        private readonly ISalesReturnRepo _salesReturnRepo;
        private readonly InventoryStockRepo _inventoryStockRepo;
        private readonly StockTransactionRepo _stockTransactionRepo;
        private readonly StockMovementRepo _stockMovementRepo;
        private readonly string _connectionString;
        private readonly trinova_erp_backend.Services.IActivityLogService _activityLogService;

        public SalesReturnUsecase(
            ISalesReturnRepo salesReturnRepo,
            InventoryStockRepo inventoryStockRepo,
            StockTransactionRepo stockTransactionRepo,
            StockMovementRepo stockMovementRepo,
            IOptions<DatabaseConnection> options,
            trinova_erp_backend.Services.IActivityLogService activityLogService)
        {
            _salesReturnRepo = salesReturnRepo;
            _inventoryStockRepo = inventoryStockRepo;
            _stockTransactionRepo = stockTransactionRepo;
            _stockMovementRepo = stockMovementRepo;
            _connectionString = options.Value.SQLServer;
            _activityLogService = activityLogService;
        }

        public async Task<List<SalesReturnHeaderDTO>> GetAllAsync()
        {
            return await _salesReturnRepo.GetAllAsync();
        }

        public async Task<SalesReturn?> GetDetailAsync(int id)
        {
            return await _salesReturnRepo.GetDetailAsync(id);
        }

        public async Task<Dictionary<int, decimal>> GetReturnableQtyAsync(int deliveryOrderId)
        {
            if (deliveryOrderId <= 0)
                throw new InvalidOperationException("Id delivery order tidak valid.");

            return await _salesReturnRepo.GetReturnableQtyAsync(deliveryOrderId);
        }

        public async Task<int> InsertSalesReturn(SalesReturn model)
        {
            if (model.Header.DeliveryOrderId <= 0)
                throw new InvalidOperationException("Delivery Order wajib dipilih.");

            if (model.Detail == null || model.Detail.Count == 0)
                throw new InvalidOperationException("Detail barang retur wajib diisi.");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var tx = connection.BeginTransaction();

            try
            {
                var returnable = await _salesReturnRepo.GetReturnableQtyAsync(
                    model.Header.DeliveryOrderId, connection, tx);

                foreach (var line in model.Detail)
                {
                    if (line.Qty <= 0)
                        throw new InvalidOperationException(
                            $"Qty retur untuk produk {line.ProductName ?? line.ProductId.ToString()} harus lebih dari 0.");

                    if (line.WarehouseId <= 0)
                        throw new InvalidOperationException(
                            $"Produk {line.ProductName ?? line.ProductId.ToString()} belum memiliki gudang.");

                    var maxReturnable = returnable.TryGetValue(line.ProductId, out var r) ? r : 0m;
                    if (line.Qty > maxReturnable)
                        throw new InvalidOperationException(
                            $"Qty retur untuk produk {line.ProductName ?? line.ProductId.ToString()} melebihi sisa yang bisa diretur (maks {maxReturnable}).");
                }

                var returnId = await _salesReturnRepo.InsertHeaderAsync(model.Header, connection, tx);

                foreach (var line in model.Detail)
                {
                    line.ReturnId = returnId;
                    await _salesReturnRepo.InsertDetailAsync(line, connection, tx);

                    await _inventoryStockRepo.AddAvailableAsync(
                        connection, tx, line.ProductId, line.WarehouseId, line.Qty);

                    await _stockTransactionRepo.CreateAsync(
                        new StockTransaction
                        {
                            product_id = line.ProductId,
                            warehouse_id = line.WarehouseId,
                            transaction_type = "IN",
                            quantity = line.Qty,
                            reference_no = model.Header.ReturnNumber,
                            reference_module = "SALES_RETURN",
                            reference_id = returnId,
                            remarks = "Stok masuk kembali dari retur penjualan",
                            created_at = DateTime.UtcNow.AddHours(7)
                        },
                        connection,
                        tx);

                    await _stockMovementRepo.InsertAsync(
                        new StockMovement
                        {
                            product_id = line.ProductId,
                            movement_type = "INBOUND",
                            quantity = line.Qty,
                            reference_number = model.Header.ReturnNumber,
                            notes = "Sales Return created",
                            movement_date = DateTime.UtcNow.AddHours(7),
                            created_at = DateTime.UtcNow.AddHours(7),
                            destination_warehouse_id = line.WarehouseId
                        },
                        connection,
                        tx);
                }

                await tx.CommitAsync();

                await _activityLogService.LogSalesAsync(
                    "sales_return_created",
                    $"Sales Return {model.Header.ReturnNumber} created",
                    "Stok dikembalikan untuk seluruh baris retur.",
                    "sales_return",
                    returnId,
                    model.Header.ReturnNumber);

                return returnId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
