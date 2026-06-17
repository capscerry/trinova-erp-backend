using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

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
        Task<SalesOrderDetailDTO?> GetSalesOrderDetail(int orderId);
    }

    public class SalesQuotationUsecase : ISalesQuotationUsecase
    {
        private readonly string _connectionString;
        private readonly ISalesQuotationRepo _salesQuotationRepo;
        public SalesQuotationUsecase(IOptionsSnapshot<DatabaseConnection> options,ISalesQuotationRepo salesQuotationRepo)
        {
            _connectionString = options.Value.SQLServer;
            _salesQuotationRepo = salesQuotationRepo;
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
                    CustomerId = quotation.CustomerId,
                    QuotationNumber = quotation.QuotationNumber,
                    QuotationDate = quotation.QuotationDate,
                    Address = quotation.Address,
                    Notes = quotation.Notes,

                    IsTaxable = quotation.IsTaxable ?? false,
                    IsTaxIncluded = quotation.IsTaxIncluded ?? false,

                    Subtotal = quotation.Subtotal ?? 0,
                    DiscountTotal = quotation.DiscountTotal ?? 0,
                };

                int quotationId = await _salesQuotationRepo.InsertQuotationHeader(header, conn, tx);
                
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

                    await _salesQuotationRepo.InsertQuotationDetail(detail, conn, tx);


                }
                await tx.CommitAsync();

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
        private readonly string _connectionString;
        public SalesOrderUsecase(ISalesOrderRepositories salesOrderRepo,IOptions<DatabaseConnection> options)
        {
            _salesOrderRepo = salesOrderRepo;
            _connectionString = options.Value.SQLServer;

        }

        public async Task<SalesOrderRequest> InsertSalesOrder(SalesOrderRequest model)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var tx = connection.BeginTransaction();

            try
            {
                // Insert header dan ambil hasil header yang sudah ada Id
                var insertedHeader = await _salesOrderRepo.InsertSalesOrderHeader(
                    model.Header,
                    connection,
                    tx
                );

                model.Header = insertedHeader;

                var insertedDetails = new List<SalesOrderDetail>();

                foreach (var detail in model.Detail)
                {
                    detail.OrderId = insertedHeader.OrderId;

                    var insertedDetail = await _salesOrderRepo.InsertSalesOrderDetail(
                        detail,
                        connection,
                        tx
                    );

                    insertedDetails.Add(insertedDetail);
                }

                model.Detail = insertedDetails;

                tx.Commit();

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
    }
}
