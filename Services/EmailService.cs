using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using trinova_erp_backend.Config;

namespace trinova_erp_backend.Services
{
    public interface IEmailService {
        Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes = null,
            string? attachmentFileName = null);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings              _settings;
        private readonly ILogger<EmailService>      _logger;

        public EmailService(IOptionsSnapshot<EmailSettings> options, ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger   = logger;
        }

        public async Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes    = null,
            string? attachmentFileName = null)
        {
            // ── 1. Validate EmailSettings before touching SMTP ────────────────
            // [DIAGNOSTIC] Log SMTP configuration (password presence only, never value)
            _logger.LogInformation(
                "[SendEmail] SMTP Config — Host: {Host} | Port: {Port} | User: {User} | " +
                "FromName: {FromName} | Password Configured: {HasPassword}",
                string.IsNullOrWhiteSpace(_settings.Host)        ? "(empty)" : _settings.Host,
                _settings.Port,
                string.IsNullOrWhiteSpace(_settings.User)        ? "(empty)" : _settings.User,
                string.IsNullOrWhiteSpace(_settings.FromName)    ? "(empty)" : _settings.FromName,
                !string.IsNullOrWhiteSpace(_settings.AppPassword));

            // [DIAGNOSTIC] Validate each required SMTP field before attempting connection
            if (string.IsNullOrWhiteSpace(_settings.Host))
                throw new InvalidOperationException(
                    "EmailSettings.Host tidak dikonfigurasi. " +
                    "Set environment variable SMTP_HOST.");

            if (_settings.Port <= 0)
                throw new InvalidOperationException(
                    $"EmailSettings.Port tidak valid: '{_settings.Port}'. " +
                    "Set environment variable SMTP_PORT ke angka yang benar (mis. 587).");

            if (string.IsNullOrWhiteSpace(_settings.User))
                throw new InvalidOperationException(
                    "EmailSettings.User tidak dikonfigurasi. " +
                    "Set environment variable SMTP_USER.");

            if (string.IsNullOrWhiteSpace(_settings.AppPassword))
                throw new InvalidOperationException(
                    "EmailSettings.AppPassword tidak dikonfigurasi. " +
                    "Set environment variable SMTP_APP_PASSWORD.");

            _logger.LogInformation("[SendEmail] SMTP config validation passed.");

            // ── 2. Build message ──────────────────────────────────────────────
            _logger.LogInformation("[SendEmail] Building MimeMessage to {ToEmail}", toEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.User));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            if (attachmentBytes != null && attachmentBytes.Length > 0
                && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                _logger.LogInformation(
                    "[SendEmail] Adding attachment '{FileName}' ({Bytes} bytes)",
                    attachmentFileName, attachmentBytes.Length);

                bodyBuilder.Attachments.Add(
                    attachmentFileName,
                    attachmentBytes,
                    ContentType.Parse("application/pdf"));
            }

            message.Body = bodyBuilder.ToMessageBody();
            _logger.LogInformation("[SendEmail] MimeMessage built successfully.");

            // ── 3. Send via SMTP ──────────────────────────────────────────────
            using var client = new SmtpClient
            {
                // Default MailKit timeout adalah 100 detik -- kalau ada
                // masalah jaringan ke Gmail SMTP, request akan menggantung
                // lama sebelum akhirnya gagal. 15 detik cukup untuk connect+
                // auth+kirim dalam kondisi normal, dan bikin kegagalan
                // jaringan langsung ketahuan alih-alih terasa "loading lama".
                Timeout = 15000,
            };
            try
            {
                _logger.LogInformation(
                    "[SendEmail] Connecting to SMTP -> {Host}:{Port} (StartTls)",
                    _settings.Host, _settings.Port);
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                _logger.LogInformation("[SendEmail] SMTP Connected.");

                _logger.LogInformation("[SendEmail] Authenticating SMTP user={User}", _settings.User);
                await client.AuthenticateAsync(_settings.User, _settings.AppPassword);
                _logger.LogInformation("[SendEmail] SMTP Authenticated.");

                _logger.LogInformation("[SendEmail] Sending Email to {ToEmail}", toEmail);
                await client.SendAsync(message);
                _logger.LogInformation("[SendEmail] Email Sent Successfully to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                // [DIAGNOSTIC] Log full exception detail so the real failure is never hidden
                _logger.LogError(
                    ex,
                    "[SendEmail] FAILED — ExceptionType: {ExceptionType} | Message: {Message} | " +
                    "InnerExceptionType: {InnerType} | InnerMessage: {InnerMessage} | " +
                    "StackTrace: {StackTrace}",
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.GetType().FullName ?? "(none)",
                    ex.InnerException?.Message          ?? "(none)",
                    ex.StackTrace);

                // Preserve original exception as inner so callers can inspect it
                throw new InvalidOperationException("Gagal Mengirim Email", ex);
            }
            finally
            {
                if (client.IsConnected)
                {
                    _logger.LogInformation("[SendEmail] Disconnecting SMTP.");
                    await client.DisconnectAsync(true);
                    _logger.LogInformation("[SendEmail] SMTP Disconnected.");
                }
            }
        }
    }
}
 
