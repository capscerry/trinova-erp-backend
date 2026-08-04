using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface IPenerimaanPenjualanUsecase
    {
        Task<List<BankDTO>> GetBankAsync();
        Task<PenerimaanPenjualan> InsertSalesReceipt(PenerimaanPenjualan dto);
        Task<bool> UpdateSalesReceipt(int id, PenerimaanPenjualan dto);
        Task<List<PenerimaanPenjualan>> GetAllSalesReceipt();
    }
    public class PenerimaanPenjualanUsecase : IPenerimaanPenjualanUsecase
    {
        private readonly IPenerimaanPenjualanRepo _penerimaanRepo;
        private readonly ISalesInvoiceRepo _salesInvoiceRepo;
        private readonly IUangMukaRepositories _uangMukaRepo;
        private readonly trinova_erp_backend.Services.IActivityLogService _activityLogService;

        public PenerimaanPenjualanUsecase(
            IPenerimaanPenjualanRepo penerimaanRepo,
            ISalesInvoiceRepo salesInvoiceRepo,
            IUangMukaRepositories uangMukaRepo,
            trinova_erp_backend.Services.IActivityLogService activityLogService)
        {
            _penerimaanRepo = penerimaanRepo;
            _salesInvoiceRepo = salesInvoiceRepo;
            _uangMukaRepo = uangMukaRepo;
            _activityLogService = activityLogService;
        }

        public async Task<List<BankDTO>> GetBankAsync()
        {
            var result = await _penerimaanRepo.GetBankDTO();
            return result;
        }
        public async Task<PenerimaanPenjualan> InsertSalesReceipt(PenerimaanPenjualan dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.NoBukti))
                    throw new Exception("No Bukti wajib diisi.");

                if (dto.CustomerId <= 0)
                    throw new Exception("Customer wajib dipilih.");

                if (dto.BankId <= 0)
                    throw new Exception("Bank wajib dipilih.");

                if (dto.NilaiPembayaran <= 0)
                    throw new Exception("Nilai pembayaran harus lebih dari 0.");

                if (dto.TanggalBayar == default)
                    dto.TanggalBayar = DateTime.UtcNow.AddHours(7);

                var result = await _penerimaanRepo.InsertSalesReceipt(dto);

                if (result == null)
                    throw new Exception("Gagal menyimpan data penerimaan penjualan.");

                await _activityLogService.LogSalesAsync(
                    "sales_receipt_created",
                    $"Sales Receipt {result.NoBukti} recorded",
                    $"Payment received from {result.CustomerName ?? dto.CustomerName ?? "customer"}.",
                    "sales_receipt",
                    result.Id,
                    result.NoBukti);

                if (result.UangMukaId.HasValue && result.UangMukaId.Value > 0)
                {
                    var downPayment = await _uangMukaRepo.GetUangMukaById(result.UangMukaId.Value);
                    if (downPayment != null)
                    {
                        await _activityLogService.LogSalesAsync(
                            "down_payment_received",
                            $"Sales Down Payment {downPayment.NoFaktur} received",
                            $"Received with receipt {result.NoBukti}.",
                            "uang_muka",
                            downPayment.Id,
                            downPayment.NoFaktur);
                    }
                }

                if (result.SalesInvoiceId.HasValue && result.SalesInvoiceId.Value > 0)
                {
                    var invoice = await _salesInvoiceRepo.GetHeaderById(result.SalesInvoiceId.Value);
                    if (invoice != null && (invoice.Status == "Paid" || invoice.Status == "Partially Paid"))
                    {
                        await _activityLogService.LogSalesAsync(
                            invoice.Status == "Paid" ? "sales_invoice_paid" : "sales_invoice_partially_paid",
                            $"Sales Invoice {invoice.InvoiceNumber} {invoice.Status.ToLower()}",
                            $"Payment recorded from receipt {result.NoBukti}.",
                            "sales_invoice",
                            invoice.Id,
                            invoice.InvoiceNumber);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Usecase InsertSalesReceipt Error: {ex.Message}", ex);
            }
        }

        public async Task<List<PenerimaanPenjualan>> GetAllSalesReceipt()
        {
            try
            {
                return await _penerimaanRepo.GetAllSalesReceipt();
            }
            catch (Exception ex)
            {
                throw new Exception($"Usecase GetAllSalesReceipt Error: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateSalesReceipt(int id, PenerimaanPenjualan dto)
        {
            try
            {
                if (id <= 0)
                    throw new Exception("Id penerimaan tidak valid.");

                if (string.IsNullOrWhiteSpace(dto.NoBukti))
                    throw new Exception("No Bukti wajib diisi.");

                if (dto.CustomerId <= 0)
                    throw new Exception("Customer wajib dipilih.");

                if (dto.BankId <= 0)
                    throw new Exception("Bank wajib dipilih.");

                if (dto.NilaiPembayaran <= 0)
                    throw new Exception("Nilai pembayaran harus lebih dari 0.");

                if (dto.TanggalBayar == default)
                    dto.TanggalBayar = DateTime.UtcNow.AddHours(7);

                var result = await _penerimaanRepo.UpdateSalesReceipt(id, dto);

                if (result)
                {
                    await _activityLogService.LogSalesAsync(
                        "sales_receipt_updated",
                        $"Sales Receipt {dto.NoBukti} updated",
                        $"Payment receipt data was updated.",
                        "sales_receipt",
                        id,
                        dto.NoBukti);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Usecase UpdateSalesReceipt Error: {ex.Message}", ex);
            }
        }
    }
}
