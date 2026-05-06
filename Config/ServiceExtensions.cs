using trinova_erp_backend.Repositories.Penjualan;
using trinova_erp_backend.Usecase.Penjualan;

namespace trinova_erp_backend.Config
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this  IServiceCollection services)
        {
            services.AddScoped<ICategoryCustomerRepo, CategoryCustomerRepo>();
            services.AddScoped<IMasterCustomerRepo,MasterCustomerRepo>();

            services.AddScoped<ICustomerUsecase, CustomerUsecase>();
            return services;
        }
    }
}
