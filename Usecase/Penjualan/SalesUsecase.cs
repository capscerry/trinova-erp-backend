using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Services;

namespace trinova_erp_backend.Usecase.Penjualan
{

    public interface ISalesCategoryUsecase
    {
        Task<string> InsertDataCategory(SalesCategory model);
        Task<List<SalesCategory>> GetAllCategory();
        Task<bool> UpdateDataCategory(SalesCategory model);
        Task<bool> UpdateStatusCategory(int id, int status);
    }
    
    public interface ISalesQuotationUsecase {
        Task<string> InsertSalesQuotation(SalesQuotation quotation);
        Task<List<QuotationHeaderDTO>> GetAllQuotations();
        Task<List<QuotationHeaderDTO>> GetAllQuotationById(int customerId);
        Task<List<QuotationDetailDTO>> GetAllQuotationDetailById(int quotationId);
        Task<QuotationHeaderDetailDTO?> GetQuotationHeaderDetailById(int quotationId);
    }

    public interface ISalesOrderUsecase
    {
        Task<SalesOrderRequest> InsertSalesOrder(SalesOrderRequest model);
        Task<List<SalesOrderHeader>> GetAllSalesOrder();
        Task<List<SalesOrderHeader>> GetSalesOrderByCustomerId(int customerId);
        Task<SalesOrderDetailDTO?> GetSalesOrderDetail(int orderId);
        Task CancelSalesOrder(int orderId);
    }

    public class SalesQuotationUsecase : ISalesQuotationUsecase
    {
        private readonly string _connectionString;
        private readonly ISalesQuotationRepo _salesQuotationRepo;
        private readonly IActivityLogService _activityLogService;
        public SalesQuotationUsecase(
            IOptionsSnapshot<DatabaseConnection> options,
            ISalesQuotationRepo salesQuotationRepo,
            IActivityLogService activityLogService)
        {
            _connectionString = options.Value.SQLServer;
            _salesQuotationRepo = salesQuotationRepo;
            _activityLogService = activityLogService;
        }
        public async Task<List<QuotationHeaderDTO>> GetAllQuotations()
        {
            var result = await _salesQuotationRepo.GetQuotationHeaders();
            return result;
        }

        public async Task<List<QuotationHeaderDTO>> GetAllQuotationById(int customerId)
        {
            var result = await _salesQuotationRepo.GetQuotationHeaderById(customerId);
            return result;
        }

        public async Task<QuotationHeaderDetailDTO?> GetQuotationHeaderDetailById(int quotationId)
        {
            if (quotationId <= 0)
                throw new ArgumentException("QuotationId tidak valid");

            return await _salesQuotationRepo.GetQuotationHeaderDetailById(quotationId);
        }
        public async Task<List<QuotationDetailDTO>> GetAllQuotationDetailById(int quotationId)
        {
            var result = await _salesQuotationRepo.GetQuotationDetailById(quotationId);
            return result;
        }

        public async Task<string> InsertSalesQuotation(SalesQuotation quotation)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
         
            try
            {

                var header = new QuotationHeader
                {
                    QuotationId = quotation?.Id ?? 0,
                    CustomerId = quotation.CustomerId,
                    QuotationNumber = quotation.QuotationNumber,
                    QuotationDate = quotation.QuotationDate,
                    Address = quotation.Address,
                    Notes = quotation.Notes,

                    IsTaxable = quotation.IsTaxable ?? false,
                    IsTaxIncluded = quotation.IsTaxIncluded ?? false,

                    Subtotal = quotation.Subtotal ?? 0,
                    DiscountTotal = quotation.DiscountTotal ?? 0,
                    TaxTotal = quotation.TaxTotal ?? 0
                };

                int quotationId = await _salesQuotationRepo.UpsertQuotationHeader(header, conn, tx);
                
                foreach(var item in quotation.Details)
                {
                    var detail = new QuotationDetail
                    {
                        QuotationId = quotationId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UomId = item.UomId,
                        Price = item.Price,
                        DiscountPercent = item.DiscountPercent ?? 0,
                        DiscountAmount = item.DiscountAmount ?? 0
                    };

                    await _salesQuotationRepo.UpsertQuotationDetail(detail, conn, tx);


                }
                await tx.CommitAsync();

                await _activityLogService.LogSalesAsync(
                    "sales_quotation_created",
                    $"Sales Quotation {quotation.QuotationNumber} created",
                    "Sales quotation created from sales module.",
                    "sales_quotation",
                    quotationId,
                    quotation.QuotationNumber);

                return "Insert Sales Quotation Success";

            }
            catch(Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception("Failed Insert", ex);
            }
        }
    }

    public class SalesCategoryUsecase : ISalesCategoryUsecase
    {
        private readonly ISalesCategoryRepo _salesCategoryRepo;

        public SalesCategoryUsecase(ISalesCategoryRepo salesCatergoryRepo)
        {
            _salesCategoryRepo = salesCatergoryRepo;
        }

        public async Task<string> InsertDataCategory(SalesCategory category)
        {
            var result = await _salesCategoryRepo.InsertCategorySales(category);
            if (result)
                return "Insert Successfully";

            return "Insert Failed";
        }

        public async Task<bool> UpdateStatusCategory(int id, int status)
        {
            var result = await _salesCategoryRepo.UpdateStatusCategory(id, status);
            return result;

        }

        public async Task<List<SalesCategory>> GetAllCategory()
        {
            var result = await _salesCategoryRepo.GetAllCategory();
            return result;
        }

        

        public async Task<bool> UpdateDataCategory(SalesCategory model)
        {
            var result = await _salesCategoryRepo.UpdateCategorySales(model);
            return result;
        }
    }

    public class SalesOrderUsecase : ISalesOrderUsecase
    {
        private readonly ISalesOrderRepositories _salesOrderRepo;
        private readonly InventoryStockRepo _inventoryStockRepo;
        private readonly string _connectionString;
        private readonly IActivityLogService _activityLogService;
        public SalesOrderUsecase(
            ISalesOrderRepositories salesOrderRepo,
            InventoryStockRepo inventoryStockRepo,
            IOptions<DatabaseConnection> options,
            IActivityLogService activityLogService
        )
        {
            _salesOrderRepo = salesOrderRepo;
            _inventoryStockRepo = inventoryStockRepo;
            _connectionString = options.Value.SQLServer;
            _activityLogService = activityLogService;

        }

        public async Task<List<SalesOrderHeader>> GetSalesOrderByCustomerId(int customerId)
        {
            var result = await _salesOrderRepo.GetSalesOrderByCustomerId(customerId);
            return result;
        }

        public async Task<SalesOrderRequest> InsertSalesOrder(SalesOrderRequest model)
        {
            // Sales order ini punya tanda tangan (belum punya OrderId) HANYA saat
            // pertama kali dibuat — bukan saat diedit. Stok cuma boleh direservasi
            // sekali, di momen pembuatan itu, supaya edit berulang tidak
            // menumpuk reservasi.
            bool isNewOrder = model.Header.OrderId <= 0;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var tx = connection.BeginTransaction();

            try
            {
                // Insert header dan ambil hasil header yang sudah ada Id
                var insertedHeader = await _salesOrderRepo.UpsertSalesOrderHeader(
                    model.Header,
                    connection,
                    tx
                );

                model.Header = insertedHeader;

                var insertedDetails = new List<SalesOrderDetail>();

                foreach (var detail in model.Detail)
                {
                    detail.OrderId = insertedHeader.OrderId;

                    var insertedDetail = await _salesOrderRepo.UpsertSalesOrderDetail(
                        detail,
                        connection,
                        tx
                    );

                    insertedDetails.Add(insertedDetail);
                }

                model.Detail = insertedDetails;

                if (isNewOrder)
                {
                    foreach (var detail in insertedDetails)
                    {
                        if (detail.WareHouseId == null || detail.WareHouseId <= 0)
                            throw new InvalidOperationException(
                                $"Produk {detail.ProductName} belum memiliki gudang, sales order tidak bisa dibuat.");

                        // Reservasi cuma menggeser qty_reserved/qty_available di
                        // inventory_stock — belum ada barang fisik yang bergerak,
                        // jadi tidak dicatat ke stock_transaction (tabel itu
                        // dibatasi CHECK constraint hanya untuk IN/OUT/TRANSFER/
                        // ADJUSTMENT, semuanya pergerakan fisik).
                        await _inventoryStockRepo.ReserveAsync(
                            connection,
                            tx,
                            detail.ProductId,
                            detail.WareHouseId.Value,
                            detail.ProductQty);
                    }
                }

                tx.Commit();

                await _activityLogService.LogSalesAsync(
                    "sales_order_created",
                    $"Sales Order {insertedHeader.SoNumber} created",
                    $"Created for {insertedHeader.CustomerName ?? "customer"}.",
                    "sales_order",
                    insertedHeader.OrderId,
                    insertedHeader.SoNumber);

                return model;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<List<SalesOrderHeader>> GetAllSalesOrder()
        {
            return await _salesOrderRepo.GetAllSalesOrder();
        }

        public async Task<SalesOrderDetailDTO?> GetSalesOrderDetail(int orderId)
        {
            return await _salesOrderRepo.GetSalesOrderDetail(orderId);
        }

        public async Task CancelSalesOrder(int orderId)
        {
            var detail = await _salesOrderRepo.GetSalesOrderDetail(orderId);

            if (detail == null)
                throw new InvalidOperationException("Sales order tidak ditemukan.");

            if (string.Equals(detail.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Sales order dengan status '{detail.Status}' tidak bisa dibatalkan.");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var tx = connection.BeginTransaction();

            try
            {
                var shipped = await _salesOrderRepo.GetShippedQuantitiesAsync(orderId, connection, tx);

                foreach (var line in detail.Detail)
                {
                    var shippedQty = shipped.TryGetValue(line.ProductId, out var s) ? s : 0m;
                    var remaining = line.ProductQty - shippedQty;

                    if (remaining > 0 && line.WareHouseId.HasValue && line.WareHouseId > 0)
                    {
                        await _inventoryStockRepo.ReleaseReservedAsync(
                            connection,
                            tx,
                            line.ProductId,
                            line.WareHouseId.Value,
                            remaining);
                    }
                }

                await _salesOrderRepo.SetSalesOrderCancelledAsync(orderId, connection, tx);

                await tx.CommitAsync();

                await _activityLogService.LogSalesAsync(
                    "sales_order_cancelled",
                    $"Sales Order {detail.SoNumber} cancelled",
                    "Reservasi stok untuk bagian yang belum dikirim sudah dilepas.",
                    "sales_order",
                    orderId,
                    detail.SoNumber);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
