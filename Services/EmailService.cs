using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
//using System.Net.Mail;
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
        private readonly EmailSettings _settings;
        public EmailService(IOptionsSnapshot<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(string toEmail,
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

            var builder = new BodyBuilder { HtmlBody = htmlBody };

            if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrWhiteSpace(attachmentFileName))
                builder.Attachments.Add(attachmentFileName, attachmentBytes, ContentType.Parse("application/pdf"));

            message.Body = builder.ToMessageBody();
            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.User, _settings.AppPassword);
                await client.SendAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gagal mengirim email ke {ToEmail}", toEmail);
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
 