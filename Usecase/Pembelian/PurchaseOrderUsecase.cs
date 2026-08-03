using trinova_erp_backend.Models;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Repositories.Pembelian;
using trinova_erp_backend.Services;

namespace trinova_erp_backend.Usecase.Pembelian
{
    public interface IPurchaseOrderUsecase
    {
        Task<string> GetNextPONumber();

        Task<int> InsertPurchaseOrder(PurchaseOrder model);

        Task<List<PurchaseOrder>> GetAllPurchaseOrder();

        Task<List<PurchaseOrder>> GetPurchaseOrdersByStatus(string status);

        Task<List<PurchaseOrder>> GetApprovedAndCompletedPurchaseOrders();

        Task<PurchaseOrder?> GetPurchaseOrderById(int id);

        Task<bool> UpdatePurchaseOrder(PurchaseOrder model);

        Task<bool> DeletePurchaseOrder(int id);

        Task<(bool success, string message)> RequestApproval(int id);

        Task<(bool success, string message)> ApprovePurchaseOrder(int id);

        Task<(bool success, string message)> RejectPurchaseOrder(int id);

        Task<bool> UnapprovePurchaseOrder(int id);

        Task SendPurchaseOrderEmailAsync(int id, SendQuotationEmailRequest? request);

        Task<PurchaseOrderPrintDetailDTO?> GetPurchaseOrderPrintDetailAsync(int id);
    }

    public class PurchaseOrderUsecase : IPurchaseOrderUsecase
    {
        private readonly IPurchaseOrderRepo       _purchaseOrderRepo;
        private readonly ISupplierRepo            _supplierRepo;
        private readonly IPurchaseOrderDetailRepo _purchaseOrderDetailRepo;
        private readonly ISupplierProductRepo     _supplierProductRepo;
        private readonly IEmailService            _emailService;
        private readonly IActivityLogService      _activityLogService;
        private readonly ILogger<PurchaseOrderUsecase> _logger;

        public PurchaseOrderUsecase(
            IPurchaseOrderRepo       purchaseOrderRepo,
            ISupplierRepo            supplierRepo,
            IPurchaseOrderDetailRepo purchaseOrderDetailRepo,
            ISupplierProductRepo     supplierProductRepo,
            IEmailService            emailService,
            IActivityLogService      activityLogService,
            ILogger<PurchaseOrderUsecase> logger
        )
        {
            _purchaseOrderRepo       = purchaseOrderRepo;
            _supplierRepo            = supplierRepo;
            _purchaseOrderDetailRepo = purchaseOrderDetailRepo;
            _supplierProductRepo     = supplierProductRepo;
            _emailService            = emailService;
            _activityLogService      = activityLogService;
            _logger                  = logger;
        }

        public async Task<PurchaseOrderPrintDetailDTO?> GetPurchaseOrderPrintDetailAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Id Purchase Order tidak valid.");

            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);
            if (po == null)
                return null;

            var supplier = await _supplierRepo.GetSupplierById(po.supplier_id);
            var details  = await _purchaseOrderDetailRepo.GetDetailsWithProductByPurchaseOrderId(id);

            return new PurchaseOrderPrintDetailDTO
            {
                Header   = po,
                Supplier = supplier,
                Details  = details
            };
        }

        public async Task SendPurchaseOrderEmailAsync(int id, SendQuotationEmailRequest? request)
        {
            _logger.LogInformation("[SendEmail] Request received — PO id={Id}", id);

            if (id <= 0)
                throw new ArgumentException("Id Purchase Order tidak valid.");

            // ── Step 1: Load Purchase Order ───────────────────────────────────
            _logger.LogInformation("[SendEmail] Loading Purchase Order id={Id}", id);
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);
            if (po == null)
                throw new InvalidOperationException("Purchase Order tidak ditemukan.");
            _logger.LogInformation(
                "[SendEmail] Purchase Order loaded — po_number={PoNumber} supplier_id={SupplierId} status={Status}",
                po.po_number, po.supplier_id, po.status);

            if (!string.Equals(po.status, "Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Purchase Order '{po.po_number}' harus berstatus Approved sebelum email dapat dikirim ke supplier. Status saat ini: {po.status}.");

            // ── Step 2: Load Supplier ─────────────────────────────────────────
            _logger.LogInformation("[SendEmail] Loading Supplier id={SupplierId}", po.supplier_id);
            var supplier = await _supplierRepo.GetSupplierById(po.supplier_id);
            if (supplier == null)
                throw new InvalidOperationException("Data supplier untuk Purchase Order ini tidak ditemukan.");
            _logger.LogInformation(
                "[SendEmail] Supplier loaded — name={SupplierName} email={Email}",
                supplier.supplier_name, supplier.email ?? "(null)");

            if (string.IsNullOrWhiteSpace(supplier.email))
                throw new InvalidOperationException(
                    $"Supplier '{supplier.supplier_name}' belum memiliki alamat email terdaftar. " +
                    "Lengkapi data email supplier terlebih dahulu di menu Supplier.");

            // ── Step 3: Decode attachment ─────────────────────────────────────
            byte[]? attachmentBytes = null;
            if (!string.IsNullOrWhiteSpace(request?.AttachmentBase64))
            {
                _logger.LogInformation(
                    "[SendEmail] Decoding PDF attachment — FileName={FileName} Base64Length={Len}",
                    request.AttachmentFileName ?? "(null)", request.AttachmentBase64.Length);
                try
                {
                    attachmentBytes = Convert.FromBase64String(request.AttachmentBase64);
                    _logger.LogInformation(
                        "[SendEmail] PDF decoded successfully — {Bytes} bytes", attachmentBytes.Length);
                }
                catch (FormatException fex)
                {
                    _logger.LogError(fex,
                        "[SendEmail] FAILED at attachment decode — base64 string is malformed");
                    throw new InvalidOperationException("Lampiran PDF tidak valid (base64 rusak).");
                }
            }
            else
            {
                _logger.LogInformation("[SendEmail] No attachment provided — sending email without PDF.");
            }

            // ── Step 4: Build email body ──────────────────────────────────────
            _logger.LogInformation("[SendEmail] Building HTML email body.");
            var htmlBody = BuildPurchaseOrderEmailHtml(po, supplier, request?.Message);
            var subject  = $"Purchase Order {po.po_number} — Trinova";
            var fileName = string.IsNullOrWhiteSpace(request?.AttachmentFileName)
                ? $"PO-{po.po_number}.pdf"
                : request.AttachmentFileName;
            _logger.LogInformation(
                "[SendEmail] Email prepared — Subject={Subject} To={ToEmail} FileName={FileName}",
                subject, supplier.email, fileName);

            // ── Step 5: Send via EmailService ─────────────────────────────────
            _logger.LogInformation("[SendEmail] Entering EmailService.SendAsync");
            await _emailService.SendAsync(supplier.email!, supplier.supplier_name, subject, htmlBody, attachmentBytes, fileName);
            _logger.LogInformation("[SendEmail] EmailService.SendAsync returned — email dispatched.");

            // ── Step 6: Activity log ──────────────────────────────────────────
            _logger.LogInformation("[SendEmail] Logging activity.");
            await _activityLogService.LogAsync(new ActivityLogCreate
            {
                Module        = "purchasing",
                ActivityType  = "purchase_order_email_sent",
                Title         = $"Purchase Order {po.po_number} dikirim via email ke {supplier.email}",
                Description   = request?.Message,
                RefTable      = "purchase_order",
                RefId         = po.purchase_order_id,
                RefNumber     = po.po_number
            });

            _logger.LogInformation("[SendEmail] Completed Successfully — PO {PoNumber} sent to {Email}",
                po.po_number, supplier.email);
        }

        private static string BuildPurchaseOrderEmailHtml(PurchaseOrder po, Supplier supplier, string? customMessage)
        {
            var messageBlock = string.IsNullOrWhiteSpace(customMessage)
                ? ""
                : $"<p style='color:#334155;'>{System.Net.WebUtility.HtmlEncode(customMessage)}</p>";

            var expectedDateText = po.expected_date.HasValue
                ? po.expected_date.Value.ToString("dd MMMM yyyy")
                : "-";

            return $@"
                <div style='font-family:Arial,sans-serif;max-width:640px;margin:0 auto;color:#1e293b;'>
                    <div style='background:#0f172a;padding:20px 24px;border-radius:8px 8px 0 0;'>
                        <h2 style='color:#fbbf24;margin:0;'>Trinova ERP</h2>
                        <p style='color:#cbd5e1;margin:4px 0 0;font-size:13px;'>Purchase Order</p>
                    </div>
                    <div style='border:1px solid #e2e8f0;border-top:none;padding:24px;border-radius:0 0 8px 8px;'>
                        <p>Yth. Bapak/Ibu <b>{supplier.supplier_name}</b>,</p>
                        {messageBlock}
                        <p>Bersama ini kami sampaikan Purchase Order <b>{po.po_number}</b> tanggal
                           {(po.order_date.HasValue ? po.order_date.Value.ToString("dd MMMM yyyy") : "-")}
                           dalam bentuk PDF terlampir. Mohon konfirmasi ketersediaan barang dan
                           perkiraan tanggal kirim (target kami: <b>{expectedDateText}</b>).</p>
                        <p style='margin-top:12px;font-size:14px;'>
                            Total Nilai PO: <b>Rp {po.total_amount:N0}</b>
                        </p>
                        <p style='margin-top:16px;font-size:13px;color:#334155;'>
                            Silakan hubungi kami apabila ada pertanyaan mengenai pesanan ini. Terima kasih.
                        </p>
                    </div>
                </div>";
        }

        public async Task<string> GetNextPONumber()
        {
            return await _purchaseOrderRepo.GeneratePONumber();
        }

        public async Task<int> InsertPurchaseOrder(PurchaseOrder model)
        {
            var supplier =
                await _supplierRepo.GetSupplierById(model.supplier_id);

            if (supplier == null)
                return 0;

            if (supplier.status != "Active")
                return 0;

            model.created_at = DateTime.Now;
            model.status     = "Draft";
            model.po_number  = await _purchaseOrderRepo.GeneratePONumber();

            return await _purchaseOrderRepo.InsertPurchaseOrder(model);
        }

        public async Task<List<PurchaseOrder>> GetAllPurchaseOrder()
        {
            return await _purchaseOrderRepo.GetAllPurchaseOrder();
        }

        public async Task<List<PurchaseOrder>> GetPurchaseOrdersByStatus(string status)
        {
            return await _purchaseOrderRepo.GetPurchaseOrdersByStatus(status);
        }

        public async Task<List<PurchaseOrder>> GetApprovedAndCompletedPurchaseOrders()
        {
            return await _purchaseOrderRepo.GetApprovedAndCompletedPurchaseOrders();
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderById(int id)
        {
            return await _purchaseOrderRepo.GetPurchaseOrderById(id);
        }

        public async Task<bool> UpdatePurchaseOrder(PurchaseOrder model)
        {
            var existing =
                await _purchaseOrderRepo.GetPurchaseOrderById(model.purchase_order_id);

            if (existing != null && existing.status == "Completed")
                return false;

            return await _purchaseOrderRepo.UpdatePurchaseOrder(model);
        }

        public async Task<bool> DeletePurchaseOrder(int id)
        {
            var existing =
                await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (existing != null && existing.status == "Approved")
                return false;

            return await _purchaseOrderRepo.DeletePurchaseOrder(id);
        }

        // ─── REQUEST APPROVAL ─────────────────────────────────────────────
        // Transitions Draft → Waiting for Approval.
        // Allows the requester to submit the PO for manager review.

        public async Task<(bool success, string message)> RequestApproval(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status != "Draft")
                return (false, $"Cannot request approval: Purchase Order is currently '{po.status}'. Only Draft POs can be submitted for approval.");

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            if (details.Count == 0)
                return (false, $"Cannot request approval: Purchase Order (id={id}) has no detail lines.");

            po.status = "Waiting for Approval";
            var updated = await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return updated
                ? (true, "Purchase Order submitted for approval")
                : (false, "Failed to update Purchase Order status");
        }

        public async Task<(bool success, string message)> ApprovePurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status == "Approved")
                return (false, "Purchase Order is already approved");

            if (po.status == "Completed")
                return (false, "Purchase Order is already completed");

            if (po.status != "Draft" && po.status != "Waiting for Approval")
                return (false, $"Cannot approve: Purchase Order is currently '{po.status}'");

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            if (details.Count == 0)
                return (false, $"Purchase Order (id={id}, po_number={po.po_number}) has no detail lines");

            // Hard-reserve stock for each line. Track what was actually deducted
            // so we can roll back if a later line has insufficient stock.
            var deducted = new List<(int productId, int quantity)>();

            foreach (var line in details)
            {
                var result = await _supplierProductRepo
                    .DeductStock(line.product_id, po.supplier_id, line.quantity);

                switch (result)
                {
                    case DeductStockResult.Deducted:
                        deducted.Add((line.product_id, line.quantity));
                        break;

                    case DeductStockResult.RowNotFound:
                        break;

                    case DeductStockResult.InsufficientStock:
                        foreach (var (pid, qty) in deducted)
                            await _supplierProductRepo
                                .RestoreStock(pid, po.supplier_id, qty);

                        return (false,
                            $"Insufficient stock for product_id {line.product_id}. Approval cancelled.");
                }
            }

            // All lines processed — flip status to Approved.
            po.status = "Approved";
            await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return (true, "Purchase Order approved and stock reserved");
        }

        public async Task<(bool success, string message)> RejectPurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null)
                return (false, "Purchase Order not found");

            if (po.status != "Waiting for Approval")
                return (false, $"Cannot reject: Purchase Order is currently '{po.status}'. Only POs awaiting approval can be rejected.");

            po.status = "Draft";
            var updated = await _purchaseOrderRepo.UpdatePurchaseOrder(po);

            return updated
                ? (true, "Purchase Order rejected and returned to Draft")
                : (false, "Failed to update Purchase Order status");
        }

        // ─── UNAPPROVE (undo reservation) ────────────────────────────────
        // Reverses the hard reserve and returns status to Draft.

        public async Task<bool> UnapprovePurchaseOrder(int id)
        {
            var po = await _purchaseOrderRepo.GetPurchaseOrderById(id);

            if (po == null || po.status != "Approved")
                return false;

            var details =
                await _purchaseOrderDetailRepo.GetDetailsByPurchaseOrderId(id);

            foreach (var line in details)
                await _supplierProductRepo
                    .RestoreStock(line.product_id, po.supplier_id, line.quantity);

            po.status = "Draft";
            return await _purchaseOrderRepo.UpdatePurchaseOrder(po);
        }
    }
}
