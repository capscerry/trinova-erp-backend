using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories.Pembelian
{
    public interface IPurchaseOrderRepositories
    {
        Task<List<PurchaseOrder>> GetAll();
    }
    public class PurchaseOrderRepositories
    {
        private readonly string _connectionString;

        public PurchaseOrderRepositories(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer;
        }

    }
}
