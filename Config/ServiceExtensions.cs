using trinova_erp_backend.Repositories.Pembelian;
using trinova_erp_backend.Repositories.Penjualan;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Repositories.User;
using trinova_erp_backend.Usecase;
using trinova_erp_backend.Usecase.Pembelian;
using trinova_erp_backend.Usecase.Penjualan;
using trinova_erp_backend.Usecase.Persediaan;

namespace trinova_erp_backend.Config
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services
        )
        {
            // PENJUALAN REPOSITORY
            services.AddScoped<IMasterCustomerRepo, MasterCustomerRepo>();
            services.AddScoped<ICategoryCustomerRepo, CategoryCustomerRepo>();
            services.AddScoped<ISalesQuotationRepo, SalesQuotationRepo>();
            services.AddScoped<ISalesCategoryRepo, SalesCategoryRepo>();
            services.AddScoped<ISalesOrderRepositories,SalesOrderRepositories>();
            services.AddScoped<IUangMukaRepositories, UangMukaRepositories>();
            services.AddScoped<IPenerimaanPenjualanRepo, PenerimaanPenjualanRepo>();
            services.AddScoped<IPengirimanPenjualanRepo, PengirimanPenjualanRepo>();
            services.AddScoped<ISalesInvoiceRepo, SalesInvoiceRepo>();
            services.AddScoped<ISalesDashboardRepo, SalesDashboardRepo>();
            // PENJUALAN USECASE
            services.AddScoped<ICustomerUsecase, CustomerUsecase>();
            services.AddScoped<ISalesQuotationUsecase, SalesQuotationUsecase>();
            services.AddScoped<ISalesCategoryUsecase, SalesCategoryUsecase>();
            services.AddScoped<ISalesOrderUsecase, SalesOrderUsecase>();
            services.AddScoped<IUangMukaUsecase, UangMukaUsecase>();
            services.AddScoped<IPenerimaanPenjualanUsecase, PenerimaanPenjualanUsecase>();
            services.AddScoped<IPengirimanPenjualanUsecase, PengirimanPenjualanUsecase>();
            services.AddScoped<ISalesInvoiceUsecase, SalesInvoiceUsecase>();
            services.AddScoped<ISalesDashboardUsecase, SalesDashboardUsecase>();

            // PEMBELIAN REPOSITORY
            services.AddScoped<ISupplierRepo, SupplierRepo>();
            services.AddScoped<ISupplierCategoryRepo, SupplierCategoryRepo>();
            services.AddScoped<IPurchaseOrderRepo, PurchaseOrderRepo>();
            services.AddScoped<IPurchaseOrderDetailRepo, PurchaseOrderDetailRepo>();
            services.AddScoped<IGoodsReceiptRepo, GoodsReceiptRepo>();
            services.AddScoped<IGoodsReceiptDetailRepo, GoodsReceiptDetailRepo>();
            services.AddScoped<IPurchasingDashboardRepo, PurchasingDashboardRepo>();
            services.AddScoped<ISupplierProductRepo, SupplierProductRepo>();
            services.AddScoped<IPurchaseInvoiceRepo, PurchaseInvoiceRepo>();
            services.AddScoped<IPurchaseDownPaymentRepo, PurchaseDownPaymentRepo>();
            services.AddScoped<IPurchasePaymentRepo, PurchasePaymentRepo>();

            // PEMBELIAN USECASE
            services.AddScoped<ISupplierUsecase, SupplierUsecase>();
            services.AddScoped<ISupplierCategoryUsecase, SupplierCategoryUsecase>();
            services.AddScoped<IPurchaseOrderUsecase, PurchaseOrderUsecase>();
            services.AddScoped<IPurchaseOrderDetailUsecase, PurchaseOrderDetailUsecase>();
            services.AddScoped<IGoodsReceiptUsecase, GoodsReceiptUsecase>();
            services.AddScoped<IGoodsReceiptDetailUsecase, GoodsReceiptDetailUsecase>();
            services.AddScoped<IPurchasingDashboardUsecase, PurchasingDashboardUsecase>();
            services.AddScoped<ISupplierProductUsecase, SupplierProductUsecase>();
            services.AddScoped<IPurchaseInvoiceUsecase, PurchaseInvoiceUsecase>();
            services.AddScoped<IPurchaseDownPaymentUsecase, PurchaseDownPaymentUsecase>();
            services.AddScoped<IPurchasePaymentUsecase, PurchasePaymentUsecase>();

            // PERSEDIAAN REPOSITORY
            services.AddScoped<MasterProductRepo>();
            services.AddScoped<MasterProductCategoryRepo>();
            services.AddScoped<IMasterUomRepo, MasterUomRepo>();
            services.AddScoped<MasterWarehouseRepo>();
            services.AddScoped<InventoryStockRepo>();
            services.AddScoped<StockTransactionRepo>();
            services.AddScoped<PurchaseRequisitionRepo>();

            // PERSEDIAAN USECASE
            services.AddScoped<IMasterProductUsecase, MasterProductUsecase>();
            services.AddScoped<IMasterProductCategoryUsecase, MasterProductCategoryUsecase>();
            services.AddScoped<IMasterUomUsecase, MasterUomUsecase>();
            services.AddScoped<IMasterWarehouseUsecase, MasterWarehouseUsecase>();
            services.AddScoped<InventoryStockUsecase>();
            services.AddScoped<StockTransactionUsecase>();
            services.AddScoped<PurchaseRequisitionUsecase>();



            // USER 
            services.AddScoped<IMasterUserRepositories, MasterUserRepositories>();
            services.AddScoped<IMasterUserUsecase, MasterUserUsecase>();




            
            return services;
        }
    }
}
