using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Models.Persediaan;
using trinova_erp_backend.Repositories.Penjualan;
using trinova_erp_backend.Repositories.Persediaan;
using trinova_erp_backend.Services;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface ISalesInvoiceUsecase
    {
        Task<List<SalesInvoiceHeader>> GetAll();
        Task<List<SalesInvoiceHeader>> GetOutstanding();
        Task<List<SalesInvoiceHeader>> GetByCustomerId(int customerId);
        Task<SalesInvoice?> GetById(int id);
        Task<SalesInvoice> Create(SalesInvoice model);
        Task<SalesInvoice> Update(int id, SalesInvoice model);
        Task Delete(int id);
        Task Confirm(int id);
        Task SendInvoiceEmailAsync(int id, SendQuotationEmailRequest? request);
    }

    public class SalesInvoiceUsecase : ISalesInvoiceUsecase
    {
        private readonly ISalesInvoiceRepo _salesInvoiceRepo;
        private readonly InventoryStockRepo _inventoryStockRepo;
        private readonly StockTransactionRepo _stockTransactionRepo;
        private readonly StockMovementRepo _stockMovementRepo;
        private readonly string _connectionString;
        private readonly trinova_erp_backend.Services.IActivityLogService _activityLogService;
        private readonly IEmailService _emailService;

        public SalesInvoiceUsecase(
            ISalesInvoiceRepo salesInvoiceRepo,
            InventoryStockRepo inventoryStockRepo,
            StockTransactionRepo stockTransactionRepo,
            StockMovementRepo stockMovementRepo,
            IOptions<DatabaseConnection> options,
            trinova_erp_backend.Services.IActivityLogService activityLogService,
            IEmailService emailService)
        {
            _salesInvoiceRepo = salesInvoiceRepo;
            _inventoryStockRepo = inventoryStockRepo;
            _stockTransactionRepo = stockTransactionRepo;
            _stockMovementRepo = stockMovementRepo;
            _connectionString = options.Value.SQLServer!;
            _activityLogService = activityLogService;
            _emailService = emailService;
        }

        public async Task SendInvoiceEmailAsync(int id, SendQuotationEmailRequest? request)
        {
            if (id <= 0)
                throw new ArgumentException("Id faktur tidak valid.");

            var invoice = await GetById(id);
            if (invoice?.Header == null)
                throw new InvalidOperationException("Faktur penjualan tidak ditemukan.");

            var header = invoice.Header;
            if (string.IsNullOrWhiteSpace(header.CustomerEmail))
                throw new InvalidOperationException(
                    $"Pelanggan '{header.CustomerName}' belum memiliki alamat email terdaftar. " +
                    "Lengkapi data email pelanggan terlebih dahulu di menu Customer.");

            byte[]? attachmentBytes = null;
            if (!string.IsNullOrWhiteSpace(request?.AttachmentBase64))
            {
                try
                {
                    attachmentBytes = Convert.FromBase64String(request.AttachmentBase64);
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("Lampiran PDF tidak valid (base64 rusak).");
                }
            }

            var htmlBody = BuildInvoiceEmailHtml(header, request?.Message);
            var subject  = $"Faktur Penjualan {header.InvoiceNumber} — Trinova";
            var fileName = string.IsNullOrWhiteSpace(request?.AttachmentFileName)
                ? $"Invoice-{header.InvoiceNumber}.pdf"
                : request.AttachmentFileName;

            await _emailService.SendAsync(
                header.CustomerEmail!,
                header.CustomerName ?? "Pelanggan",
                subject,
                htmlBody,
                attachmentBytes,
                fileName);

            await _activityLogService.LogSalesAsync(
                "invoice_email_sent",
                $"Invoice {header.InvoiceNumber} dikirim via email ke {header.CustomerEmail}",
                request?.Message,
                "sales_invoice",
                header.Id,
                header.InvoiceNumber);
        }

        private static string BuildInvoiceEmailHtml(SalesInvoiceHeader header, string? customMessage)
        {
            var messageBlock = string.IsNullOrWhiteSpace(customMessage)
                ? ""
                : $"<p style='color:#334155;'>{System.Net.WebUtility.HtmlEncode(customMessage)}</p>";

            var statusNote = header.RemainingAmount > 0
                ? $"<p style='margin:2px 0;color:#dc2626;'>Sisa tagihan: Rp {header.RemainingAmount:N0} (jatuh tempo {header.DueDate:dd MMMM yyyy})</p>"
                : "<p style='margin:2px 0;color:#166534;'>Faktur ini sudah lunas.</p>";

            return $@"
                <div style='font-family:Arial,sans-serif;max-width:640px;margin:0 auto;color:#1e293b;'>
                    <div style='background:#0f172a;padding:20px 24px;border-radius:8px 8px 0 0;'>
                        <h2 style='color:#fbbf24;margin:0;'>Trinova ERP</h2>
                        <p style='color:#cbd5e1;margin:4px 0 0;font-size:13px;'>Faktur Penjualan / Sales Invoice</p>
                    </div>
                    <div style='border:1px solid #e2e8f0;border-top:none;padding:24px;border-radius:0 0 8px 8px;'>
                        <p>Yth. Bapak/Ibu <b>{header.CustomerName}</b>,</p>
                        {messageBlock}
                        <p>Berikut kami lampirkan faktur penjualan <b>{header.InvoiceNumber}</b> tanggal
                           {header.InvoiceDate:dd MMMM yyyy} dalam bentuk PDF terlampir.</p>
                        <div style='margin-top:12px;font-size:14px;'>
                            <p style='margin:2px 0;'>Total Tagihan: <b>Rp {header.GrandTotal:N0}</b></p>
                            {statusNote}
                        </div>
                        <p style='margin-top:16px;font-size:13px;color:#334155;'>
                            Silakan hubungi kami apabila ada pertanyaan mengenai faktur ini. Terima kasih.
                        </p>
                    </div>
                </div>";
        }

        public async Task<List<SalesInvoiceHeader>> GetAll()
        {
            return await _salesInvoiceRepo.GetAll();
        }

        public async Task<List<SalesInvoiceHeader>> GetOutstanding()
        {
            return await _salesInvoiceRepo.GetOutstanding();
        }

        public async Task<List<SalesInvoiceHeader>> GetByCustomerId(int customerId)
        {
            if (customerId <= 0)
                throw new Exception("Customer tidak valid.");

            return await _salesInvoiceRepo.GetByCustomerId(customerId);
        }

        public async Task<SalesInvoice?> GetById(int id)
        {
            if (id <= 0)
                throw new Exception("Id faktur tidak valid.");

            var header = await _salesInvoiceRepo.GetHeaderById(id);
            if (header == null)
                return null;

            var detail = await _salesInvoiceRepo.GetDetailByInvoiceId(id);

            return new SalesInvoice
            {
                Header = header,
                Detail = detail
            };
        }

        public async Task<SalesInvoice> Create(SalesInvoice model)
        {
            ValidateInvoice(model);
            NormalizeInvoice(model);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var invoiceId = await _salesInvoiceRepo.InsertHeader(model.Header, connection, transaction);

                // Invoice ini sudah punya SO/DO di belakangnya => stoknya sudah
                // ditangani di sana (reserve saat SO, deduct saat DO). Cuma
                // invoice yang berdiri sendiri (cash sale/direct sale) yang
                // perlu memotong stok sendiri di sini.
                var isDirectSale = model.Header.SalesOrderId == null && model.Header.DeliveryOrderId == null;

                foreach (var detail in model.Detail)
                {
                    detail.SalesInvoiceId = invoiceId;
                    await _salesInvoiceRepo.InsertDetail(detail, connection, transaction);

                    if (!isDirectSale || detail.WarehouseId == null || detail.WarehouseId <= 0 || detail.Quantity <= 0)
                        continue;

                    // Baris tanpa gudang dianggap jasa (tidak ada stok fisik) —
                    // cuma baris dengan gudang terisi yang memotong stok.
                    await _inventoryStockRepo.DeductAvailableAsync(
                        connection,
                        transaction,
                        detail.ProductId,
                        detail.WarehouseId.Value,
                        detail.Quantity);

                    await _stockTransactionRepo.CreateAsync(
                        new StockTransaction
                        {
                            product_id = detail.ProductId,
                            warehouse_id = detail.WarehouseId.Value,
                            transaction_type = "OUT",
                            quantity = detail.Quantity,
                            reference_no = model.Header.InvoiceNumber,
                            reference_module = "SALES_INVOICE",
                            reference_id = invoiceId,
                            remarks = "Stok keluar saat Sales Invoice (cash sale) dibuat",
                            created_at = DateTime.Now
                        },
                        connection,
                        transaction);

                    await _stockMovementRepo.InsertAsync(
                        new StockMovement
                        {
                            product_id = detail.ProductId,
                            movement_type = "OUTBOUND",
                            quantity = detail.Quantity,
                            reference_number = model.Header.InvoiceNumber,
                            notes = "Sales Invoice (cash sale) created",
                            movement_date = DateTime.Now,
                            created_at = DateTime.Now,
                            source_warehouse_id = detail.WarehouseId.Value
                        },
                        connection,
                        transaction);
                }

                await UpdateRelatedDocumentStatuses(model.Header, connection, transaction);

                await transaction.CommitAsync();

                await _activityLogService.LogSalesAsync(
                    "sales_invoice_created",
                    $"Sales Invoice {model.Header.InvoiceNumber} created",
                    $"Invoice created for {model.Header.CustomerName ?? "customer"}.",
                    "sales_invoice",
                    invoiceId,
                    model.Header.InvoiceNumber);

                if (model.Header.Status == "Paid" || model.Header.Status == "Partially Paid")
                {
                    await _activityLogService.LogSalesAsync(
                        model.Header.Status == "Paid" ? "sales_invoice_paid" : "sales_invoice_partially_paid",
                        $"Sales Invoice {model.Header.InvoiceNumber} {model.Header.Status.ToLower()}",
                        $"Paid amount Rp {model.Header.PaidAmount:N0}.",
                        "sales_invoice",
                        invoiceId,
                        model.Header.InvoiceNumber);
                }

                var created = await GetById(invoiceId);
                return created!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SalesInvoice> Update(int id, SalesInvoice model)
        {
            if (id <= 0)
                throw new Exception("Id faktur tidak valid.");

            var existing = await _salesInvoiceRepo.GetHeaderById(id);
            if (existing == null)
                throw new Exception("Faktur penjualan tidak ditemukan.");

            if (existing.Status == "Paid" || existing.Status == "Lunas")
                throw new Exception("Faktur yang sudah lunas tidak dapat diubah.");

            model.Header.Id = id;
            model.Header.PaidAmount = existing.PaidAmount;
            ValidateInvoice(model);
            NormalizeInvoice(model);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                await _salesInvoiceRepo.UpdateHeader(model.Header, connection, transaction);
                await _salesInvoiceRepo.DeleteDetailByInvoiceId(id, connection, transaction);

                foreach (var detail in model.Detail)
                {
                    detail.SalesInvoiceId = id;
                    await _salesInvoiceRepo.InsertDetail(detail, connection, transaction);
                }

                await UpdateRelatedDocumentStatuses(model.Header, connection, transaction);

                await transaction.CommitAsync();

                var updated = await GetById(id);
                return updated!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task Delete(int id)
        {
            if (id <= 0)
                throw new Exception("Id faktur tidak valid.");

            var existing = await _salesInvoiceRepo.GetHeaderById(id);
            if (existing == null)
                throw new Exception("Faktur penjualan tidak ditemukan.");

            if (existing.PaidAmount > 0 || existing.Status == "Paid" || existing.Status == "Lunas")
                throw new Exception("Faktur yang sudah memiliki pembayaran tidak dapat dihapus.");

            await _salesInvoiceRepo.DeleteInvoice(id);
        }

        public async Task Confirm(int id)
        {
            if (id <= 0)
                throw new Exception("Id faktur tidak valid.");

            var existing = await _salesInvoiceRepo.GetHeaderById(id);
            if (existing == null)
                throw new Exception("Faktur penjualan tidak ditemukan.");

            await _salesInvoiceRepo.ConfirmInvoice(id);
        }

        private static void ValidateInvoice(SalesInvoice model)
        {
            if (model.Header == null)
                throw new Exception("Header faktur wajib diisi.");

            if (string.IsNullOrWhiteSpace(model.Header.InvoiceNumber))
                throw new Exception("Nomor faktur wajib diisi.");

            if (model.Header.CustomerId <= 0)
                throw new Exception("Customer wajib dipilih.");

            if (model.Header.InvoiceDate == default)
                throw new Exception("Tanggal faktur wajib diisi.");

            if (model.Header.DueDate == default)
                throw new Exception("Tanggal jatuh tempo wajib diisi.");

            if (model.Header.DueDate.Date < model.Header.InvoiceDate.Date)
                throw new Exception("Tanggal jatuh tempo tidak boleh sebelum tanggal faktur.");

            if (model.Detail == null || !model.Detail.Any())
                throw new Exception("Detail barang faktur wajib diisi.");

            foreach (var detail in model.Detail)
            {
                if (detail.ProductId <= 0)
                    throw new Exception("Product wajib diisi pada setiap detail faktur.");

                if (detail.Quantity <= 0)
                    throw new Exception("Quantity harus lebih dari 0.");

                if (detail.Price < 0)
                    throw new Exception("Harga tidak boleh negatif.");

                if (detail.Discount < 0)
                    throw new Exception("Diskon tidak boleh negatif.");

                if (detail.Tax < 0)
                    throw new Exception("Pajak tidak boleh negatif.");
            }
        }

        private static void NormalizeInvoice(SalesInvoice model)
        {
            var subtotal = model.Detail.Sum(d => d.Quantity * d.Price);
            var discountTotal = model.Detail.Sum(d => d.Discount);
            var taxTotal = model.Detail.Sum(d => d.Tax);

            foreach (var detail in model.Detail)
            {
                var gross = detail.Quantity * detail.Price;
                detail.Subtotal = gross - detail.Discount + detail.Tax;
                detail.Description = string.IsNullOrWhiteSpace(detail.Description)
                    ? detail.ProductName
                    : detail.Description;
            }

            var header = model.Header;
            header.Subtotal = subtotal;
            header.DiscountTotal = discountTotal;
            header.TaxTotal = taxTotal;
            header.DownPaymentAmount = Math.Max(0, header.DownPaymentAmount);
            header.ShippingCost = Math.Max(0, header.ShippingCost);
            header.PaidAmount = Math.Max(0, header.PaidAmount);
            header.GrandTotal = Math.Max(
                0,
                header.Subtotal
                - header.DiscountTotal
                + header.TaxTotal
                + header.ShippingCost
                - header.DownPaymentAmount
            );
            header.RemainingAmount = Math.Max(0, header.GrandTotal - header.PaidAmount);

            if (string.IsNullOrWhiteSpace(header.Status))
                header.Status = "Issued";

            if (header.RemainingAmount <= 0 && header.GrandTotal > 0)
                header.Status = "Paid";
            else if (header.PaidAmount > 0)
                header.Status = "Partially Paid";
            else if (header.Status == "Draft")
                header.Status = "Issued";
        }

        // Flow baru: Delivery Order sekarang SELALU dibuat setelah invoice
        // (bukan sebelumnya), jadi status DO tidak lagi berubah menjadi
        // "Invoiced" di sini -- vocabulary status DO yang baru
        // (In Delivery/Received/Cancelled) tidak punya nilai "Invoiced" sama
        // sekali. Untuk Sales Order, status "Invoiced"/"Partially Paid" juga
        // sudah dipensiunkan -- begitu invoice pertama dibuat untuk SO
        // tersebut, SO cukup pindah dari "Belum Diproses" ke "Diproses"
        // (satu arah, tidak menimpa status yang sudah lebih maju seperti
        // In Delivery/Completed/Cancelled kalau ada invoice susulan/koreksi).
        private static async Task UpdateRelatedDocumentStatuses(
            SalesInvoiceHeader header,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            if (header.SalesOrderId.HasValue && header.SalesOrderId.Value > 0)
            {
                const string updateSalesOrderQuery = @"
                    UPDATE sales_order
                    SET status = 'Diproses'
                    WHERE order_id = @SalesOrderId AND status = 'Belum Diproses';";

                await connection.ExecuteAsync(
                    updateSalesOrderQuery,
                    new { SalesOrderId = header.SalesOrderId.Value },
                    transaction);
            }
        }
    }
}
