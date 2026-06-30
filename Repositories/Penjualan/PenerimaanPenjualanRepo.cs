using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace trinova_erp_backend.Repositories.Penjualan
{
    public interface IPenerimaanPenjualanRepo {
        Task<List<BankDTO>> GetBankDTO();
        Task<PenerimaanPenjualan> InsertSalesReceipt(PenerimaanPenjualan dto);

        Task<List<PenerimaanPenjualan>> GetAllSalesReceipt();

    }

    public class PenerimaanPenjualanRepo : IPenerimaanPenjualanRepo
    {
        private readonly string _connectionString;
        public PenerimaanPenjualanRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task<List<BankDTO>> GetBankDTO()
        {
            string query = @"SELECT 
                                id AS Id,
                                bank_name AS BankName,
                                account_number AS BankAccount
                             FROM bank";

            using var connection = new SqlConnection(_connectionString);
            var result = await connection.QueryAsync<BankDTO>(query);

            return result.ToList();
        }

        public async Task<List<PenerimaanPenjualan>> GetAllSalesReceipt()
        {
            try
            {
                string query = @"
            SELECT
                sr.id AS Id,
                sr.no_bukti AS NoBukti,
                sr.customer_id AS CustomerId,
                mc.customer_name AS CustomerName,
                sr.bank_id AS BankId,
                b.bank_name AS BankName,
                sr.nilai_pembayaran AS NilaiPembayaran,
                sr.tanggal_bayar AS TanggalBayar,
                sr.uang_muka_id AS UangMukaId,
                sr.sales_order_id AS SalesOrderId,
                sr.sales_invoice_id AS SalesInvoiceId,
                ISNULL(sr.status, 'Draft') AS Status
            FROM sales_receipt sr
            INNER JOIN master_customer mc
                ON sr.customer_id = mc.customer_id
            INNER JOIN bank b
                ON sr.bank_id = b.id
            ORDER BY sr.id DESC
        ";

                using var connection = new SqlConnection(_connectionString);

                var result = await connection.QueryAsync<PenerimaanPenjualan>(query);

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"GetAllSalesReceipt Error: {ex.Message}", ex);
            }
        }

        public async Task<PenerimaanPenjualan> InsertSalesReceipt(PenerimaanPenjualan dto)
        {
            string query = @"
        INSERT INTO sales_receipt
        (
            no_bukti,
            customer_id,
            bank_id,
            nilai_pembayaran,
            tanggal_bayar,
            uang_muka_id,
            sales_order_id,
            sales_invoice_id,
            status
        )
        OUTPUT
            INSERTED.id,
            INSERTED.no_bukti AS NoBukti,
            INSERTED.customer_id AS CustomerId,
            INSERTED.bank_id AS BankId,
            INSERTED.nilai_pembayaran AS NilaiPembayaran,
            INSERTED.tanggal_bayar AS TanggalBayar,
            INSERTED.uang_muka_id AS UangMukaId,
            INSERTED.sales_order_id AS SalesOrderId,
            INSERTED.sales_invoice_id AS SalesInvoiceId,
            INSERTED.status AS Status
        VALUES
        (
            @NoBukti,
            @CustomerId,
            @BankId,
            @NilaiPembayaran,
            @TanggalBayar,
            @UangMukaId,
            @SalesOrderId,
            @SalesInvoiceId,
            'Validated'
        );
    ";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<PenerimaanPenjualan>(
                    query,
                    new
                    {
                        dto.NoBukti,
                        dto.CustomerId,
                        dto.BankId,
                        dto.NilaiPembayaran,
                        dto.TanggalBayar,
                        UangMukaId = dto.UangMukaId == 0 ? null : dto.UangMukaId,
                        SalesOrderId = dto.SalesOrderId == 0 ? null : dto.SalesOrderId,
                        SalesInvoiceId = dto.SalesInvoiceId == 0 ? null : dto.SalesInvoiceId
                    },
                    transaction);

                await ApplyPaymentToOutstandingInvoices(dto, connection, transaction);

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task ApplyPaymentToOutstandingInvoices(
            PenerimaanPenjualan dto,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            var remainingPayment = dto.NilaiPembayaran;
            if (remainingPayment <= 0)
                return;

            const string outstandingQuery = @"
                SELECT
                    id AS Id,
                    remaining_amount AS RemainingAmount
                FROM sales_invoice
                WHERE customer_id = @CustomerId
                  AND remaining_amount > 0
                  AND status NOT IN ('Paid', 'Cancelled', 'Lunas', 'Dibatalkan')
                  AND (
                        (
                            @SalesInvoiceId IS NOT NULL
                            AND id = @SalesInvoiceId
                        )
                        OR (
                            @SalesInvoiceId IS NULL
                            AND (
                                @SalesOrderId IS NULL
                                OR sales_order_id = @SalesOrderId
                            )
                        )
                  )
                ORDER BY due_date ASC, id ASC;";

            var invoices = await connection.QueryAsync<OutstandingInvoicePaymentTarget>(
                outstandingQuery,
                new
                {
                    dto.CustomerId,
                    SalesOrderId = dto.SalesOrderId == 0 ? null : dto.SalesOrderId,
                    SalesInvoiceId = dto.SalesInvoiceId == 0 ? null : dto.SalesInvoiceId
                },
                transaction);

            foreach (var invoice in invoices)
            {
                if (remainingPayment <= 0)
                    break;

                var paymentApplied = Math.Min(remainingPayment, invoice.RemainingAmount);
                remainingPayment -= paymentApplied;

                const string updateInvoiceQuery = @"
                    UPDATE sales_invoice
                    SET
                        paid_amount = ISNULL(paid_amount, 0) + @PaymentApplied,
                        remaining_amount = CASE
                            WHEN ISNULL(remaining_amount, 0) - @PaymentApplied <= 0 THEN 0
                            ELSE ISNULL(remaining_amount, 0) - @PaymentApplied
                        END,
                        status = CASE
                            WHEN ISNULL(remaining_amount, 0) - @PaymentApplied <= 0 THEN 'Paid'
                            ELSE 'Partially Paid'
                        END,
                        updated_at = GETDATE()
                    WHERE id = @InvoiceId;";

                await connection.ExecuteAsync(
                    updateInvoiceQuery,
                    new
                    {
                        InvoiceId = invoice.Id,
                        PaymentApplied = paymentApplied
                    },
                    transaction);
            }
        }

        private sealed class OutstandingInvoicePaymentTarget
        {
            public int Id { get; set; }
            public decimal RemainingAmount { get; set; }
        }
    }
}
