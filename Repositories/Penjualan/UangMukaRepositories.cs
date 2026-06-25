using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface IUangMukaRepositories
    {
        Task<string> GenerateNoFaktur();

        Task<bool> InsertUangMuka(UangMuka data);
        Task<UangMuka> GetUangMukaById(int id);

        //Task<UangMuka> GetAllUangMuka();
        Task<IEnumerable<UangMuka>> GetAllUangMuka();
        
    }
    public class UangMukaRepositories : IUangMukaRepositories
    {
        private readonly string _connectionString;
        public UangMukaRepositories(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<string> GenerateNoFaktur()
        {
            const string query = @"
                SELECT TOP 1 NoFaktur
                FROM uang_muka
                ORDER BY Id DESC";

            using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            string? lastFaktur =
                await connection
                    .ExecuteScalarAsync<string>(query);

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastFaktur))
            {
                string numericPart =
                    lastFaktur.Replace("UM", "");

                if (int.TryParse(numericPart, out int parsed))
                    nextNumber = parsed + 1;
            }

            return $"UM{nextNumber:D6}";
        }

        public async Task<IEnumerable<UangMuka>> GetAllUangMuka()
        {
            string query = @" SELECT 
                                 um.Id,
                                 um.NoFaktur,
                                 um.Tanggal,
                                 um.CustomerId,
                                 mc.customer_name AS CustomerName,
                                 um.NoSo As SoNumber,
                                 um.NoPO,
                                 um.NominalUangMuka,
                                 um.TotalAmount
                             FROM uang_muka as um join master_customer as mc
                             ON um.CustomerId = mc.customer_id ";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryAsync<UangMuka>(query);

            return result;
        }

        public async Task<UangMuka> GetUangMukaById(int id)
        {
            string query = @"
        SELECT 
            um.Id,
            um.NoFaktur,
            um.Tanggal,
            um.CustomerId,
            mc.customer_name AS CustomerName,
            um.NoPO,
            um.NoSo AS SoNumber,
            um.NominalUangMuka,
            um.IsTaxable,
            um.IsTaxIncluded,
            um.TaxAmount,
            um.TotalAmount,
            um.SyaratPembayaran,
            um.Alamat,
            um.Keterangan,
            um.CreatedAt,
            um.UpdatedAt,
            um.CreatedBy,
            um.UpdatedBy
        FROM uang_muka um
        LEFT JOIN master_customer mc 
            ON um.CustomerId = mc.customer_id
        WHERE um.Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var result = await connection.QueryFirstOrDefaultAsync<UangMuka>(
                query,
                new { Id = id }
            );

            return result!;
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
                    NoSo,
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
                    @SoNumber,
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
