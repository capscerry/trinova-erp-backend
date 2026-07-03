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
        Task<bool> InsertUangMuka(UangMuka data);
        Task<bool> UpdateUangMuka(UangMuka data);
        Task<UangMuka> GetUangMukaById(int id);
        Task MarkAsReceived(int id, SqlConnection connection, SqlTransaction transaction);
        Task UpdatePaymentStatus(int id, SqlConnection connection, SqlTransaction transaction);

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
                                 um.TotalAmount,
                                 ISNULL(um.Status, 'Draft') AS Status
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
            ISNULL(um.Status, 'Draft') AS Status,
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
                    Status,
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
                    @NoSo,
                    @Alamat,
                    @Keterangan,
                    @Status,
                    @CreatedBy
                )";

            using var connection = new SqlConnection(_connectionString);

            await connection.OpenAsync();   

            var result = await connection.ExecuteAsync(query, data);

            return result > 0;
        }

        public async Task<bool> UpdateUangMuka(UangMuka data)
        {
            const string query = @"
                UPDATE uang_muka
                SET
                    NoFaktur = @NoFaktur,
                    Tanggal = @Tanggal,
                    CustomerId = @CustomerId,
                    NoPO = @NoPO,
                    NominalUangMuka = @NominalUangMuka,
                    IsTaxable = @IsTaxable,
                    IsTaxIncluded = @IsTaxIncluded,
                    TaxAmount = @TaxAmount,
                    TotalAmount = @TotalAmount,
                    SyaratPembayaran = @SyaratPembayaran,
                    NoSo = @NoSo,
                    Alamat = @Alamat,
                    Keterangan = @Keterangan,
                    Status = @Status,
                    UpdatedBy = @UpdatedBy,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var result = await connection.ExecuteAsync(query, data);
            return result > 0;
        }

        public async Task MarkAsReceived(int id, SqlConnection connection, SqlTransaction transaction)
        {
            const string query = @"
                UPDATE uang_muka
                SET
                    Status = 'Received',
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;";

            await connection.ExecuteAsync(query, new { Id = id }, transaction);
        }

        public async Task UpdatePaymentStatus(int id, SqlConnection connection, SqlTransaction transaction)
        {
            const string query = @"
                UPDATE uang_muka
                SET
                    Status = CASE
                        WHEN (
                            SELECT ISNULL(SUM(nilai_pembayaran), 0)
                            FROM sales_receipt
                            WHERE uang_muka_id = @Id
                              AND ISNULL(status, '') NOT IN ('Cancelled', 'Dibatalkan')
                        ) >= ISNULL(NULLIF(TotalAmount, 0), NominalUangMuka)
                            THEN 'Received'
                        WHEN (
                            SELECT ISNULL(SUM(nilai_pembayaran), 0)
                            FROM sales_receipt
                            WHERE uang_muka_id = @Id
                              AND ISNULL(status, '') NOT IN ('Cancelled', 'Dibatalkan')
                        ) > 0
                            THEN 'Partially Paid'
                        ELSE 'Draft'
                    END,
                    UpdatedAt = GETDATE()
                WHERE Id = @Id;";

            await connection.ExecuteAsync(query, new { Id = id }, transaction);
        }
    }
}
