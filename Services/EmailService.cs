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
            _logger.LogInformation(
                "EmailService.SendAsync: Host={Host} Port={Port} User={User} FromName={FromName}",
                _settings.Host,
                _settings.Port,
                _settings.User,
                _settings.FromName);

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

            // ── 2. Build message ──────────────────────────────────────────────
            _logger.LogInformation("EmailService: membuat MimeMessage ke {ToEmail}", toEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.User));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            if (attachmentBytes != null && attachmentBytes.Length > 0
                && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                _logger.LogInformation(
                    "EmailService: menambahkan lampiran '{FileName}' ({Bytes} bytes)",
                    attachmentFileName, attachmentBytes.Length);

                bodyBuilder.Attachments.Add(
                    attachmentFileName,
                    attachmentBytes,
                    ContentType.Parse("application/pdf"));
            }

            message.Body = bodyBuilder.ToMessageBody();

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
                    "EmailService: SMTP Connect -> {Host}:{Port} (StartTls)",
                    _settings.Host, _settings.Port);
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);

                _logger.LogInformation("EmailService: SMTP Authenticate user={User}", _settings.User);
                await client.AuthenticateAsync(_settings.User, _settings.AppPassword);

                _logger.LogInformation("EmailService: SMTP Send ke {ToEmail}", toEmail);
                await client.SendAsync(message);

                _logger.LogInformation("EmailService: email berhasil dikirim ke {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                // FIX: previously this line crashed with FormatException
                // ("Failure to parse near offset 25. Expected an ASCII digit.")
                // because Console.WriteLine("... {ToEmail}", toEmail) used a
                // named placeholder that String.Format cannot parse.
                // Now we log with structured logging (which handles named tokens
                // correctly) and chain the original exception so the real cause
                // is never swallowed.
                _logger.LogError(ex, "EmailService: gagal mengirim email ke {ToEmail}", toEmail);
                throw new InvalidOperationException("Gagal Mengirim Email", ex);
            }
            finally
            {
                if (client.IsConnected)
                {
                    _logger.LogInformation("EmailService: SMTP Disconnect");
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}
 
