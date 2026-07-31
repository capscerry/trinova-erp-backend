using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using trinova_erp_backend.Config;

namespace trinova_erp_backend.Services
{
    public interface IEmailService
    {
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
        private readonly EmailSettings _settings;

        public EmailService(IOptionsSnapshot<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes = null,
            string? attachmentFileName = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.User) || string.IsNullOrWhiteSpace(_settings.AppPassword))
                throw new InvalidOperationException("SMTP belum dikonfigurasi");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.User));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
                bodyBuilder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse("application/pdf"));

            message.Body = bodyBuilder.ToMessageBody();

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
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.User, _settings.AppPassword);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gagal mengirim email ke {0}: {1}", toEmail, ex.Message);
                throw new InvalidOperationException("Gagal Mengirim Email");
            }
            finally
            {
                if (client.IsConnected)
                    await client.DisconnectAsync(true);
            }
        }
    }
}
