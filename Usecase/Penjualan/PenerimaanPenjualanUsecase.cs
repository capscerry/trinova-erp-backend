using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

namespace trinova_erp_backend.Usecase.Penjualan
{
    public interface IPenerimaanPenjualanUsecase
    {
        Task<List<BankDTO>> GetBankAsync();
        Task<PenerimaanPenjualan> InsertSalesReceipt(PenerimaanPenjualan dto);
        Task<List<PenerimaanPenjualan>> GetAllSalesReceipt();
    }
    public class PenerimaanPenjualanUsecase : IPenerimaanPenjualanUsecase
    {
        private readonly IPenerimaanPenjualanRepo _penerimaanRepo;
        public PenerimaanPenjualanUsecase(IPenerimaanPenjualanRepo penerimaanRepo)
        {
            _penerimaanRepo = penerimaanRepo;
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
                    dto.TanggalBayar = DateTime.Now;

                var result = await _penerimaanRepo.InsertSalesReceipt(dto);

                if (result == null)
                    throw new Exception("Gagal menyimpan data penerimaan penjualan.");

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
    }
}
