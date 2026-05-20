using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface IUangMukaRepositories
    {
        Task<bool> InsertUangMuka(UangMuka data);
    }
    public class UangMukaRepositories : IUangMukaRepositories
    {
        private readonly string _connectionString;
        public UangMukaRepositories(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<bool> InsertUangMuka(UangMuka data)
        {
            string query = @"
                INSERT INTO uang_muka (
                    NoFaktur,
                    Tanggal,
                    CustomerId,
                    NoPO,
                    NominalUangMuka,
                    IsTaxable,
                    IsTaxIncluded,
                    TaxAmount,
                    TotalAmount,
                    SyaratPembayaran,
                    Alamat,
                    Keterangan,
                    CreatedBy
                ) VALUES (
                    @NoFaktur,
                    @Tanggal,
                    @CustomerId,
                    @NoPO,
                    @NominalUangMuka,
                    @IsTaxable,
                    @IsTaxIncluded,
                    @TaxAmount,
                    @TotalAmount,
                    @SyaratPembayaran,
                    @Alamat,
                    @Keterangan,
                    @CreatedBy
                )";

            using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(query, data);

            return result > 0;
        }
    }
}
