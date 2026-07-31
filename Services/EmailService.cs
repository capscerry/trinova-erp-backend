using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
            // ══════════════════════════════════════════════════════════════════
            // TASK 6 — Validate SMTP Configuration
            // ══════════════════════════════════════════════════════════════════
            _logger.LogInformation(
                "[SMTP-DIAG] ── CONFIG CHECK ──────────────────────────────────────\n" +
                "  Host              : {Host}\n" +
                "  Port              : {Port}\n" +
                "  Username          : {User}\n" +
                "  Password Set      : {HasPassword}\n" +
                "  FromName          : {FromName}\n" +
                "  TLS Mode          : StartTls (fixed)\n" +
                "──────────────────────────────────────────────────────────────────",
                string.IsNullOrWhiteSpace(_settings.Host)     ? "(EMPTY - will fail)" : _settings.Host,
                _settings.Port <= 0                           ? $"{_settings.Port} (INVALID)" : _settings.Port.ToString(),
                string.IsNullOrWhiteSpace(_settings.User)     ? "(EMPTY - will fail)" : _settings.User,
                !string.IsNullOrWhiteSpace(_settings.AppPassword),
                string.IsNullOrWhiteSpace(_settings.FromName) ? "(EMPTY)" : _settings.FromName);

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

            _logger.LogInformation("[SMTP-DIAG] Config validation passed.");

            // ══════════════════════════════════════════════════════════════════
            // TASK 2 — DNS Resolution
            // ══════════════════════════════════════════════════════════════════
            bool dnsResolved  = false;
            bool tcpReachable = false;
            bool tlsStarted   = false;
            bool authReached  = false;

            _logger.LogInformation(
                "[SMTP-DIAG] ── DNS RESOLUTION ────────────────────────────────────\n" +
                "  Resolving host: {Host} at {Time}",
                _settings.Host, DateTime.UtcNow.ToString("O"));

            var dnsStopwatch = Stopwatch.StartNew();
            IPAddress[] resolvedAddresses;
            try
            {
                resolvedAddresses = await Dns.GetHostAddressesAsync(_settings.Host);
                dnsStopwatch.Stop();
                dnsResolved = true;

                var addressList = string.Join("\n    ", (IEnumerable<IPAddress>)resolvedAddresses);
                _logger.LogInformation(
                    "[SMTP-DIAG] DNS resolved in {ElapsedMs} ms\n" +
                    "  {Host} resolved to:\n    {Addresses}",
                    dnsStopwatch.ElapsedMilliseconds,
                    _settings.Host,
                    string.IsNullOrEmpty(addressList) ? "(no addresses returned)" : addressList);

                if (resolvedAddresses.Length == 0)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: DNS lookup returned 0 addresses for {Host}. " +
                        "This may indicate a DNS configuration issue in the deployment environment.",
                        _settings.Host);
            }
            catch (Exception dnsEx)
            {
                dnsStopwatch.Stop();
                _logger.LogError(
                    dnsEx,
                    "[SMTP-DIAG] DNS FAILED after {ElapsedMs} ms — " +
                    "ExceptionType: {ExType} | Message: {Message}",
                    dnsStopwatch.ElapsedMilliseconds,
                    dnsEx.GetType().FullName,
                    dnsEx.Message);

                // Surface clearly — still propagate for infrastructure report
                _logger.LogError(
                    "[SMTP-DIAG] INFRASTRUCTURE REPORT:\n" +
                    "  DNS Resolved    : NO\n" +
                    "  TCP Reachable   : UNKNOWN (DNS failed before TCP test)\n" +
                    "  TLS Started     : UNKNOWN\n" +
                    "  Auth Reached    : UNKNOWN\n" +
                    "  Failure Stage   : DNS\n" +
                    "  Likely Cause    : DNS not reachable inside deployment container, " +
                    "or hostname is wrong.\n" +
                    "  Recommendation  : Verify SMTP_HOST value and container DNS config.");

                throw new InvalidOperationException("Gagal Mengirim Email", dnsEx);
            }

            // ══════════════════════════════════════════════════════════════════
            // TASK 1 & 3 — Raw TCP Connectivity Test + Timing
            // ══════════════════════════════════════════════════════════════════
            _logger.LogInformation(
                "[SMTP-DIAG] ── TCP CONNECTIVITY TEST ────────────────────────────\n" +
                "  Attempting TCP to {Host}:{Port} at {Time}",
                _settings.Host, _settings.Port, DateTime.UtcNow.ToString("O"));

            var tcpStopwatch = Stopwatch.StartNew();
            try
            {
                using var tcp = new TcpClient();

                // Use a 10-second cancellation so the pre-flight test itself
                // does not hang the request indefinitely.
                using var tcpCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await tcp.ConnectAsync(_settings.Host, _settings.Port, tcpCts.Token);

                tcpStopwatch.Stop();
                tcpReachable = true;

                _logger.LogInformation(
                    "[SMTP-DIAG] TCP connection established in {ElapsedMs} ms " +
                    "to {Host}:{Port}",
                    tcpStopwatch.ElapsedMilliseconds,
                    _settings.Host,
                    _settings.Port);
            }
            catch (OperationCanceledException tcpTimeout)
            {
                tcpStopwatch.Stop();
                _logger.LogError(
                    tcpTimeout,
                    "[SMTP-DIAG] TCP TIMEOUT after {ElapsedMs} ms — " +
                    "Could not reach {Host}:{Port} within 10 seconds.\n" +
                    "  ExceptionType : {ExType}\n" +
                    "  Message       : {Message}",
                    tcpStopwatch.ElapsedMilliseconds,
                    _settings.Host,
                    _settings.Port,
                    tcpTimeout.GetType().FullName,
                    tcpTimeout.Message);

                _logger.LogError(
                    "[SMTP-DIAG] INFRASTRUCTURE REPORT:\n" +
                    "  DNS Resolved    : {DnsOk}\n" +
                    "  TCP Reachable   : NO (timeout)\n" +
                    "  TLS Started     : NO\n" +
                    "  Auth Reached    : NO\n" +
                    "  Failure Stage   : TCP connect (timeout)\n" +
                    "  Elapsed         : {ElapsedMs} ms\n" +
                    "  Likely Cause    : Outbound port {Port} is blocked by the deployment " +
                    "environment (e.g. Railway does not allow outbound SMTP on port 587 by default).\n" +
                    "  Recommendations :\n" +
                    "    1. Try port 465 (SMTPS/SSL) as Railway may allow it.\n" +
                    "    2. Enable outbound networking / SMTP in Railway project settings.\n" +
                    "    3. Use a transactional email relay (SendGrid, Resend, Mailgun) " +
                    "that accepts HTTP API calls — not raw SMTP.\n" +
                    "    4. Confirm no IPv6/IPv4 mismatch by checking resolved IPs above.",
                    dnsResolved,
                    tcpStopwatch.ElapsedMilliseconds,
                    _settings.Port);

                throw new InvalidOperationException("Gagal Mengirim Email", tcpTimeout);
            }
            catch (SocketException tcpEx)
            {
                tcpStopwatch.Stop();
                _logger.LogError(
                    tcpEx,
                    "[SMTP-DIAG] TCP FAILED after {ElapsedMs} ms — " +
                    "ExceptionType: {ExType} | SocketErrorCode: {SocketError} | Message: {Message}\n" +
                    "  StackTrace: {StackTrace}",
                    tcpStopwatch.ElapsedMilliseconds,
                    tcpEx.GetType().FullName,
                    tcpEx.SocketErrorCode,
                    tcpEx.Message,
                    tcpEx.StackTrace);

                _logger.LogError(
                    "[SMTP-DIAG] INFRASTRUCTURE REPORT:\n" +
                    "  DNS Resolved    : {DnsOk}\n" +
                    "  TCP Reachable   : NO (socket error: {SocketError})\n" +
                    "  TLS Started     : NO\n" +
                    "  Auth Reached    : NO\n" +
                    "  Failure Stage   : TCP connect\n" +
                    "  Elapsed         : {ElapsedMs} ms\n" +
                    "  Likely Cause    : Connection refused, network policy, or firewall " +
                    "blocking port {Port}.\n" +
                    "  Recommendations :\n" +
                    "    1. Try port 465 (SMTPS/SSL).\n" +
                    "    2. Verify outbound networking is permitted in Railway settings.\n" +
                    "    3. Consider HTTP-based relay (SendGrid, Resend, Mailgun).",
                    dnsResolved,
                    tcpEx.SocketErrorCode,
                    tcpStopwatch.ElapsedMilliseconds,
                    _settings.Port);

                throw new InvalidOperationException("Gagal Mengirim Email", tcpEx);
            }
            catch (Exception tcpEx)
            {
                tcpStopwatch.Stop();
                _logger.LogError(
                    tcpEx,
                    "[SMTP-DIAG] TCP FAILED (unexpected) after {ElapsedMs} ms — " +
                    "ExceptionType: {ExType} | Message: {Message}\n" +
                    "  StackTrace: {StackTrace}",
                    tcpStopwatch.ElapsedMilliseconds,
                    tcpEx.GetType().FullName,
                    tcpEx.Message,
                    tcpEx.StackTrace);

                throw new InvalidOperationException("Gagal Mengirim Email", tcpEx);
            }

            // ══════════════════════════════════════════════════════════════════
            // TASK 7 — Attachment / Message Size Logging
            // ══════════════════════════════════════════════════════════════════
            _logger.LogInformation("[SMTP-DIAG] ── MESSAGE CONSTRUCTION ─────────────────────────────");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.User));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            int htmlBodyBytes = System.Text.Encoding.UTF8.GetByteCount(htmlBody ?? string.Empty);
            _logger.LogInformation(
                "[SMTP-DIAG] HTML body size : {HtmlBytes} bytes ({HtmlKb:F1} KB)",
                htmlBodyBytes, htmlBodyBytes / 1024.0);

            if (attachmentBytes != null && attachmentBytes.Length > 0
                && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                _logger.LogInformation(
                    "[SMTP-DIAG] Attachment     : '{FileName}' | {Bytes} bytes ({Kb:F1} KB)",
                    attachmentFileName,
                    attachmentBytes.Length,
                    attachmentBytes.Length / 1024.0);

                if (attachmentBytes.Length > 5 * 1024 * 1024)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: Attachment exceeds 5 MB ({Mb:F2} MB). " +
                        "Large attachments may cause SMTP timeout on slow or restricted connections.",
                        attachmentBytes.Length / (1024.0 * 1024.0));

                bodyBuilder.Attachments.Add(
                    attachmentFileName,
                    attachmentBytes,
                    ContentType.Parse("application/pdf"));
            }
            else
            {
                _logger.LogInformation("[SMTP-DIAG] Attachment     : (none)");
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Measure serialized MimeMessage size
            try
            {
                using var sizeStream = new System.IO.MemoryStream();
                await message.WriteToAsync(sizeStream);
                long mimeMessageBytes = sizeStream.Length;
                _logger.LogInformation(
                    "[SMTP-DIAG] MimeMessage total size : {Bytes} bytes ({Kb:F1} KB / {Mb:F2} MB)",
                    mimeMessageBytes,
                    mimeMessageBytes / 1024.0,
                    mimeMessageBytes / (1024.0 * 1024.0));

                if (mimeMessageBytes > 10 * 1024 * 1024)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: MimeMessage exceeds 10 MB ({Mb:F2} MB). " +
                        "This is likely to trigger timeouts or rejections on Gmail SMTP.",
                        mimeMessageBytes / (1024.0 * 1024.0));
            }
            catch (Exception sizeEx)
            {
                _logger.LogWarning(
                    sizeEx,
                    "[SMTP-DIAG] Could not measure MimeMessage size: {Message}",
                    sizeEx.Message);
            }

            _logger.LogInformation("[SMTP-DIAG] MimeMessage built successfully.");

            // ══════════════════════════════════════════════════════════════════
            // TASK 4 & 5 — SMTP Stages with Timestamps + Infrastructure Tracking
            // ══════════════════════════════════════════════════════════════════
            using var client = new SmtpClient
            {
                Timeout = 15000,
            };

            var overallStopwatch = Stopwatch.StartNew();

            try
            {
                // ── ConnectAsync ─────────────────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── ConnectAsync ──────────────────────────────────\n" +
                    "  Target    : {Host}:{Port}\n" +
                    "  TLS Mode  : StartTls\n" +
                    "  Timestamp : {Time}",
                    _settings.Host, _settings.Port,
                    DateTime.UtcNow.ToString("O"));

                var connectSw = Stopwatch.StartNew();
                await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                connectSw.Stop();
                tlsStarted = true;

                _logger.LogInformation(
                    "[SMTP-DIAG] ConnectAsync succeeded in {ElapsedMs} ms | " +
                    "IsConnected={IsConnected} | IsSecure={IsSecure} | Timestamp={Time}",
                    connectSw.ElapsedMilliseconds,
                    client.IsConnected,
                    client.IsSecure,
                    DateTime.UtcNow.ToString("O"));

                // ── AuthenticateAsync ─────────────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── AuthenticateAsync ────────────────────────────\n" +
                    "  User      : {User}\n" +
                    "  Timestamp : {Time}",
                    _settings.User, DateTime.UtcNow.ToString("O"));

                authReached = true;
                var authSw = Stopwatch.StartNew();
                await client.AuthenticateAsync(_settings.User, _settings.AppPassword);
                authSw.Stop();

                _logger.LogInformation(
                    "[SMTP-DIAG] AuthenticateAsync succeeded in {ElapsedMs} ms | Timestamp={Time}",
                    authSw.ElapsedMilliseconds, DateTime.UtcNow.ToString("O"));

                // ── SendAsync ─────────────────────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── SendAsync ─────────────────────────────────────\n" +
                    "  To        : {ToEmail}\n" +
                    "  Subject   : {Subject}\n" +
                    "  Timestamp : {Time}",
                    toEmail, subject, DateTime.UtcNow.ToString("O"));

                var sendSw = Stopwatch.StartNew();
                await client.SendAsync(message);
                sendSw.Stop();

                _logger.LogInformation(
                    "[SMTP-DIAG] SendAsync succeeded in {ElapsedMs} ms | Timestamp={Time}",
                    sendSw.ElapsedMilliseconds, DateTime.UtcNow.ToString("O"));

                _logger.LogInformation(
                    "[SMTP-DIAG] ── TIMING SUMMARY ───────────────────────────────\n" +
                    "  DNS lookup   : {DnsMs} ms\n" +
                    "  TCP connect  : {TcpMs} ms\n" +
                    "  SMTP connect : {SmtpMs} ms\n" +
                    "  Auth         : {AuthMs} ms\n" +
                    "  Send         : {SendMs} ms\n" +
                    "  Total        : {TotalMs} ms",
                    dnsStopwatch.ElapsedMilliseconds,
                    tcpStopwatch.ElapsedMilliseconds,
                    connectSw.ElapsedMilliseconds,
                    authSw.ElapsedMilliseconds,
                    sendSw.ElapsedMilliseconds,
                    overallStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                overallStopwatch.Stop();

                // Detect SocketErrorCode if the inner exception is a SocketException
                var socketEx    = ex.InnerException as SocketException ?? ex as SocketException;
                var socketError = socketEx?.SocketErrorCode.ToString() ?? "(n/a)";

                _logger.LogError(
                    ex,
                    "[SMTP-DIAG] SMTP FAILED — ExceptionType: {ExType} | Message: {Message}\n" +
                    "  InnerExceptionType    : {InnerType}\n" +
                    "  InnerMessage          : {InnerMessage}\n" +
                    "  SocketErrorCode       : {SocketError}\n" +
                    "  Elapsed               : {ElapsedMs} ms\n" +
                    "  StackTrace:\n{StackTrace}",
                    ex.GetType().FullName,
                    ex.Message,
                    ex.InnerException?.GetType().FullName ?? "(none)",
                    ex.InnerException?.Message            ?? "(none)",
                    socketError,
                    overallStopwatch.ElapsedMilliseconds,
                    ex.StackTrace);

                // ── Task 4: Infrastructure report at point of failure ─────────
                _logger.LogError(
                    "[SMTP-DIAG] INFRASTRUCTURE REPORT:\n" +
                    "  DNS Resolved          : {DnsOk}\n" +
                    "  TCP Reachable         : {TcpOk}\n" +
                    "  TLS Negotiation Start : {TlsOk}\n" +
                    "  AuthenticateAsync     : {AuthOk}\n" +
                    "  Failure Stage         : {Stage}\n" +
                    "  Elapsed               : {ElapsedMs} ms\n" +
                    "  Application Logic OK  : YES (business logic unchanged)\n" +
                    "  Issue Type            : {IssueType}",
                    dnsResolved,
                    tcpReachable,
                    tlsStarted,
                    authReached,
                    !tlsStarted  ? "MailKit ConnectAsync / TLS negotiation" :
                    !authReached ? "AuthenticateAsync"                       : "SendAsync",
                    overallStopwatch.ElapsedMilliseconds,
                    !tlsStarted ? "Infrastructure (network/TLS)" :
                    !authReached ? "Credentials or account config"  : "SMTP relay / message");

                throw new InvalidOperationException("Gagal Mengirim Email", ex);
            }
            finally
            {
                if (client.IsConnected)
                {
                    _logger.LogInformation(
                        "[SMTP-DIAG] ── DisconnectAsync ──────────────────────────────\n" +
                        "  Timestamp : {Time}",
                        DateTime.UtcNow.ToString("O"));

                    var disconnectSw = Stopwatch.StartNew();
                    await client.DisconnectAsync(true);
                    disconnectSw.Stop();

                    _logger.LogInformation(
                        "[SMTP-DIAG] DisconnectAsync completed in {ElapsedMs} ms | Timestamp={Time}",
                        disconnectSw.ElapsedMilliseconds, DateTime.UtcNow.ToString("O"));
                }
            }
        }
    }
}
