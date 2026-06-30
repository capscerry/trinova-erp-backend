using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.Penjualan;
using trinova_erp_backend.Repositories.Penjualan;

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
    }

    public class SalesInvoiceUsecase : ISalesInvoiceUsecase
    {
        private readonly ISalesInvoiceRepo _salesInvoiceRepo;
        private readonly string _connectionString;

        public SalesInvoiceUsecase(
            ISalesInvoiceRepo salesInvoiceRepo,
            IOptions<DatabaseConnection> options)
        {
            _salesInvoiceRepo = salesInvoiceRepo;
            _connectionString = options.Value.SQLServer!;
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

                foreach (var detail in model.Detail)
                {
                    detail.SalesInvoiceId = invoiceId;
                    await _salesInvoiceRepo.InsertDetail(detail, connection, transaction);
                }

                await UpdateRelatedDocumentStatuses(model.Header, connection, transaction);

                await transaction.CommitAsync();

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

        private static async Task UpdateRelatedDocumentStatuses(
            SalesInvoiceHeader header,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            if (header.DeliveryOrderId.HasValue && header.DeliveryOrderId.Value > 0)
            {
                const string updateDeliveryQuery = @"
                    UPDATE delivery_order_header
                    SET status = 'Invoiced'
                    WHERE id = @DeliveryOrderId;";

                await connection.ExecuteAsync(
                    updateDeliveryQuery,
                    new { DeliveryOrderId = header.DeliveryOrderId.Value },
                    transaction);
            }

            if (header.SalesOrderId.HasValue && header.SalesOrderId.Value > 0)
            {
                const string updateSalesOrderQuery = @"
                    UPDATE sales_order
                    SET status = 'Completed'
                    WHERE order_id = @SalesOrderId;";

                await connection.ExecuteAsync(
                    updateSalesOrderQuery,
                    new { SalesOrderId = header.SalesOrderId.Value },
                    transaction);
            }
        }
    }
}
