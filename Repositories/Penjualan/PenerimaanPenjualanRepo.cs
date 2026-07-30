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
        Task<bool> UpdateSalesReceipt(int id, PenerimaanPenjualan dto);

        Task<List<PenerimaanPenjualan>> GetAllSalesReceipt();

    }

    public class PenerimaanPenjualanRepo : IPenerimaanPenjualanRepo
    {
        private readonly string _connectionString;
        private readonly IUangMukaRepositories _uangMukaRepo;

        public PenerimaanPenjualanRepo(
            IOptions<DatabaseConnection> options,
            IUangMukaRepositories uangMukaRepo)
        {
            _connectionString = options.Value.SQLServer!;
            _uangMukaRepo = uangMukaRepo;
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
                um.NoFaktur AS UangMukaNumber,
                sr.sales_order_id AS SalesOrderId,
                so.so_number AS SalesOrderNumber,
                sr.sales_invoice_id AS SalesInvoiceId,
                ISNULL(sr.status, 'Draft') AS Status
            FROM sales_receipt sr
            INNER JOIN master_customer mc
                ON sr.customer_id = mc.customer_id
            INNER JOIN bank b
                ON sr.bank_id = b.id
            LEFT JOIN sales_order so
                ON so.order_id = sr.sales_order_id
            LEFT JOIN uang_muka um
                ON um.Id = sr.uang_muka_id
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
                var uangMukaId = dto.UangMukaId.GetValueOrDefault();
                var salesInvoiceId = dto.SalesInvoiceId.GetValueOrDefault();
                var salesOrderId = await ResolveSalesOrderIdForReceipt(dto, connection, transaction);

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
                        SalesOrderId = salesOrderId > 0 ? (int?)salesOrderId : null,
                        SalesInvoiceId = dto.SalesInvoiceId == 0 ? null : dto.SalesInvoiceId
                    },
                    transaction);

                dto.SalesOrderId = salesOrderId > 0 ? salesOrderId : null;

                if (uangMukaId > 0)
                {
                    await _uangMukaRepo.UpdatePaymentStatus(uangMukaId, connection, transaction);
                }

                if (uangMukaId <= 0 || salesInvoiceId > 0)
                {
                    await ApplyPaymentToOutstandingInvoices(dto, connection, transaction);
                }

                if (salesOrderId > 0 && salesInvoiceId <= 0)
                {
                    await UpdateSalesOrderPaymentStatus(salesOrderId, connection, transaction);
                }

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task<int> ResolveSalesOrderIdForReceipt(
            PenerimaanPenjualan dto,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            var salesOrderId = dto.SalesOrderId.GetValueOrDefault();
            if (salesOrderId > 0)
            {
                return salesOrderId;
            }

            var salesInvoiceId = dto.SalesInvoiceId.GetValueOrDefault();
            if (salesInvoiceId > 0)
            {
                salesOrderId = await connection.QueryFirstOrDefaultAsync<int>(
                    @"SELECT ISNULL(sales_order_id, 0)
                      FROM sales_invoice
                      WHERE id = @SalesInvoiceId",
                    new { SalesInvoiceId = salesInvoiceId },
                    transaction);

                if (salesOrderId > 0)
                {
                    return salesOrderId;
                }
            }

            var uangMukaId = dto.UangMukaId.GetValueOrDefault();
            if (uangMukaId <= 0)
            {
                return 0;
            }

            return await connection.QueryFirstOrDefaultAsync<int>(
                @"SELECT TOP 1 ISNULL(so.order_id, 0)
                  FROM uang_muka um
                  INNER JOIN sales_order so
                      ON so.so_number = um.NoSo
                  WHERE um.Id = @UangMukaId",
                new { UangMukaId = uangMukaId },
                transaction);
        }

        public async Task<bool> UpdateSalesReceipt(int id, PenerimaanPenjualan dto)
        {
            const string query = @"
                UPDATE sales_receipt
                SET
                    no_bukti = @NoBukti,
                    customer_id = @CustomerId,
                    bank_id = @BankId,
                    nilai_pembayaran = @NilaiPembayaran,
                    tanggal_bayar = @TanggalBayar,
                    uang_muka_id = @UangMukaId,
                    sales_order_id = @SalesOrderId,
                    sales_invoice_id = @SalesInvoiceId
                WHERE id = @Id;";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var salesOrderId = await ResolveSalesOrderIdForReceipt(dto, connection, transaction);
                dto.SalesOrderId = salesOrderId > 0 ? salesOrderId : null;

                var previous = await connection.QueryFirstOrDefaultAsync<PenerimaanPenjualan>(
                    @"SELECT
                        id AS Id,
                        nilai_pembayaran AS NilaiPembayaran,
                        uang_muka_id AS UangMukaId,
                        sales_order_id AS SalesOrderId,
                        sales_invoice_id AS SalesInvoiceId
                      FROM sales_receipt
                      WHERE id = @Id",
                    new { Id = id },
                    transaction);

                var result = await connection.ExecuteAsync(
                    query,
                    new
                    {
                        Id = id,
                        dto.NoBukti,
                        dto.CustomerId,
                        dto.BankId,
                        dto.NilaiPembayaran,
                        dto.TanggalBayar,
                        UangMukaId = dto.UangMukaId == 0 ? null : dto.UangMukaId,
                        SalesOrderId = salesOrderId > 0 ? (int?)salesOrderId : null,
                        SalesInvoiceId = dto.SalesInvoiceId == 0 ? null : dto.SalesInvoiceId
                    },
                    transaction);

                if (result == 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                await RefreshRelatedPaymentStatus(previous, connection, transaction);
                if (previous?.SalesInvoiceId.GetValueOrDefault() > 0)
                {
                    await ApplySalesInvoicePaymentDelta(
                        previous.SalesInvoiceId.Value,
                        -previous.NilaiPembayaran,
                        connection,
                        transaction);
                }

                if (dto.SalesInvoiceId.GetValueOrDefault() > 0)
                {
                    await ApplySalesInvoicePaymentDelta(
                        dto.SalesInvoiceId.Value,
                        dto.NilaiPembayaran,
                        connection,
                        transaction);
                }

                await RefreshRelatedPaymentStatus(dto, connection, transaction);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task RefreshRelatedPaymentStatus(
            PenerimaanPenjualan? receipt,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            if (receipt == null)
                return;

            var uangMukaId = receipt.UangMukaId.GetValueOrDefault();
            var salesInvoiceId = receipt.SalesInvoiceId.GetValueOrDefault();
            var salesOrderId = receipt.SalesOrderId.GetValueOrDefault();

            if (uangMukaId > 0)
            {
                await _uangMukaRepo.UpdatePaymentStatus(uangMukaId, connection, transaction);
            }

            if (salesOrderId > 0 && salesInvoiceId <= 0)
            {
                await UpdateSalesOrderPaymentStatus(salesOrderId, connection, transaction);
            }
        }

        private static async Task ApplySalesInvoicePaymentDelta(
            int salesInvoiceId,
            decimal paymentDelta,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                ;WITH InvoicePayment AS (
                    SELECT
                        id,
                        grand_total,
                        CASE
                            WHEN ISNULL(paid_amount, 0) + @PaymentDelta < 0 THEN 0
                            ELSE ISNULL(paid_amount, 0) + @PaymentDelta
                        END AS NewPaidAmount
                    FROM sales_invoice
                    WHERE id = @SalesInvoiceId
                )
                UPDATE si
                SET
                    paid_amount = ip.NewPaidAmount,
                    remaining_amount = CASE
                        WHEN ip.grand_total - ip.NewPaidAmount <= 0 THEN 0
                        ELSE ip.grand_total - ip.NewPaidAmount
                    END,
                    status = CASE
                        WHEN ip.NewPaidAmount <= 0 THEN 'Unpaid'
                        WHEN ip.NewPaidAmount >= ip.grand_total THEN 'Paid'
                        ELSE 'Partially Paid'
                    END
                FROM sales_invoice si
                INNER JOIN InvoicePayment ip ON ip.id = si.id;";

            await connection.ExecuteAsync(
                query,
                new
                {
                    SalesInvoiceId = salesInvoiceId,
                    PaymentDelta = paymentDelta
                },
                transaction);

            var salesOrderId = await connection.ExecuteScalarAsync<int?>(
                @"SELECT sales_order_id FROM sales_invoice WHERE id = @SalesInvoiceId",
                new { SalesInvoiceId = salesInvoiceId },
                transaction);

            if (salesOrderId.HasValue && salesOrderId.Value > 0)
            {
                await UpdateSalesOrderInvoiceStatus(salesOrderId.Value, connection, transaction);
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
                    sales_order_id AS SalesOrderId,
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

                if (invoice.SalesOrderId.HasValue && invoice.SalesOrderId.Value > 0)
                {
                    await UpdateSalesOrderInvoiceStatus(invoice.SalesOrderId.Value, connection, transaction);
                }
            }
        }

        // Flow baru: pembayaran (baik langsung ke SO tanpa invoice, maupun
        // lewat invoice) tidak lagi menyelesaikan (Completed) Sales Order --
        // itu sekarang HANYA dipicu oleh Delivery Order ditandai diterima
        // (lihat PengirimanPenjualanUsecase.MarkDeliveryOrderReceivedAsync).
        // Kedua method di bawah cuma memastikan SO pindah/tetap di
        // "Processing" selama masih dalam tahap penagihan & pembayaran, dan
        // sengaja TIDAK menyentuh SO yang statusnya sudah "In Delivery",
        // "Completed", atau "Cancelled" (guard di WHERE clause).
        private static async Task UpdateSalesOrderPaymentStatus(
            int salesOrderId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                UPDATE sales_order
                SET status = CASE
                    WHEN (
                        SELECT ISNULL(SUM(nilai_pembayaran), 0)
                        FROM sales_receipt
                        WHERE sales_order_id = @SalesOrderId
                          AND ISNULL(status, '') NOT IN ('Cancelled', 'Dibatalkan')
                    ) > 0
                        THEN 'Processing'
                    ELSE status
                END
                WHERE order_id = @SalesOrderId
                  AND status IN ('Draft', 'Processing');";

            await connection.ExecuteAsync(query, new { SalesOrderId = salesOrderId }, transaction);
        }

        private static async Task UpdateSalesOrderInvoiceStatus(
            int salesOrderId,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            const string query = @"
                UPDATE sales_order
                SET status = 'Processing'
                WHERE order_id = @SalesOrderId
                  AND status IN ('Draft', 'Processing');";

            await connection.ExecuteAsync(query, new { SalesOrderId = salesOrderId }, transaction);
        }

        private sealed class OutstandingInvoicePaymentTarget
        {
            public int Id { get; set; }
            public int? SalesOrderId { get; set; }
            public decimal RemainingAmount { get; set; }
        }
    }
}
