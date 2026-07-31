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
    public interface IEmailService
    {
        Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes    = null,
            string? attachmentFileName = null);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings         _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptionsSnapshot<EmailSettings> options,
            ILogger<EmailService>           logger)
        {
            _settings = options.Value;
            _logger   = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TCP helpers — use Task.WhenAny so the timeout works on every .NET
        // target without relying on the CancellationToken overload that was
        // only introduced in .NET 6.
        // ─────────────────────────────────────────────────────────────────────
        private static async Task TcpConnectWithTimeoutAsync(
            string host, int port, TimeSpan timeout)
        {
            using var tcp       = new TcpClient();
            var connectTask     = tcp.ConnectAsync(host, port);
            var completed       = await Task.WhenAny(connectTask, Task.Delay(timeout));
            if (completed != connectTask)
                throw new OperationCanceledException(
                    $"TCP connect to {host}:{port} timed out after {timeout.TotalSeconds:F0} s.");
            await connectTask; // propagate any SocketException
        }

        private static async Task TcpConnectToIpWithTimeoutAsync(
            IPAddress address, int port, TimeSpan timeout)
        {
            using var tcp       = new TcpClient();
            var connectTask     = tcp.ConnectAsync(address, port);
            var completed       = await Task.WhenAny(connectTask, Task.Delay(timeout));
            if (completed != connectTask)
                throw new OperationCanceledException(
                    $"TCP connect to {address}:{port} timed out after {timeout.TotalSeconds:F0} s.");
            await connectTask;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Result record used to accumulate per-IP TCP test results.
        // ─────────────────────────────────────────────────────────────────────
        private sealed record IpTcpResult(
            IPAddress Address,
            AddressFamily Family,
            bool          Success,
            long          ElapsedMs,
            string        ErrorType,
            string        ErrorMessage);

        // ─────────────────────────────────────────────────────────────────────
        public async Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            byte[]? attachmentBytes    = null,
            string? attachmentFileName = null)
        {
            // ══════════════════════════════════════════════════════════════
            // TASK 6 (config) — Validate SMTP Configuration
            // ══════════════════════════════════════════════════════════════
            _logger.LogInformation(
                "[SMTP-DIAG] ── CONFIG CHECK ──────────────────────────────────\n" +
                "  Host         : {Host}\n"                                         +
                "  Port         : {Port}\n"                                         +
                "  Username     : {User}\n"                                         +
                "  Password Set : {HasPwd}\n"                                       +
                "  FromName     : {From}\n"                                         +
                "  TLS Mode     : StartTls (fixed)\n"                              +
                "───────────────────────────────────────────────────────────────",
                string.IsNullOrWhiteSpace(_settings.Host)        ? "(EMPTY – will fail)" : _settings.Host,
                _settings.Port <= 0                              ? $"{_settings.Port} (INVALID)" : _settings.Port.ToString(),
                string.IsNullOrWhiteSpace(_settings.User)        ? "(EMPTY – will fail)" : _settings.User,
                !string.IsNullOrWhiteSpace(_settings.AppPassword),
                string.IsNullOrWhiteSpace(_settings.FromName)    ? "(EMPTY)" : _settings.FromName);

            if (string.IsNullOrWhiteSpace(_settings.Host))
                throw new InvalidOperationException(
                    "EmailSettings.Host is not configured. Set SMTP_HOST.");
            if (_settings.Port <= 0)
                throw new InvalidOperationException(
                    $"EmailSettings.Port invalid: '{_settings.Port}'. Set SMTP_PORT (e.g. 587).");
            if (string.IsNullOrWhiteSpace(_settings.User))
                throw new InvalidOperationException(
                    "EmailSettings.User is not configured. Set SMTP_USER.");
            if (string.IsNullOrWhiteSpace(_settings.AppPassword))
                throw new InvalidOperationException(
                    "EmailSettings.AppPassword is not configured. Set SMTP_APP_PASSWORD.");

            _logger.LogInformation("[SMTP-DIAG] Config validation passed.");

            // ══════════════════════════════════════════════════════════════
            // TASK 1 — DNS Resolution
            // ══════════════════════════════════════════════════════════════
            bool dnsResolved  = false;
            bool tcpReachable = false;
            bool tlsStarted   = false;
            bool authReached  = false;

            IPAddress[] resolvedAddresses = Array.Empty<IPAddress>();

            _logger.LogInformation(
                "[SMTP-DIAG] ── DNS RESOLUTION ────────────────────────────────\n" +
                "  Host : {Host}   Time : {Time}",
                _settings.Host, DateTime.UtcNow.ToString("O"));

            var dnsSw = Stopwatch.StartNew();
            try
            {
                resolvedAddresses = await Dns.GetHostAddressesAsync(_settings.Host);
                dnsSw.Stop();
                dnsResolved = true;

                var ipv4Resolved = resolvedAddresses
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
                var ipv6Resolved = resolvedAddresses
                    .Where(a => a.AddressFamily == AddressFamily.InterNetworkV6).ToArray();

                // Build a labelled address list for the log
                var addrLines = resolvedAddresses
                    .Select(a => $"{(a.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6")}: {a}")
                    .ToArray();

                _logger.LogInformation(
                    "[SMTP-DIAG] DNS resolved in {Ms} ms — {Count} address(es)\n" +
                    "  Host: {Host}\n\n"                                           +
                    "  Resolved Addresses:\n    {Addrs}\n\n"                      +
                    "  IPv4 count : {V4}\n"                                       +
                    "  IPv6 count : {V6}",
                    dnsSw.ElapsedMilliseconds,
                    resolvedAddresses.Length,
                    _settings.Host,
                    addrLines.Length > 0 ? string.Join("\n    ", addrLines) : "(none)",
                    ipv4Resolved.Length,
                    ipv6Resolved.Length);

                if (resolvedAddresses.Length == 0)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: DNS returned 0 addresses for {Host}.",
                        _settings.Host);

                if (ipv4Resolved.Length == 0 && ipv6Resolved.Length > 0)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: Only IPv6 addresses resolved. " +
                        "Railway may drop outbound IPv6 silently.");
            }
            catch (Exception dnsEx)
            {
                dnsSw.Stop();
                _logger.LogError(
                    dnsEx,
                    "[SMTP-DIAG] DNS FAILED after {Ms} ms\n"    +
                    "  ExceptionType : {ExType}\n"              +
                    "  Message       : {Msg}",
                    dnsSw.ElapsedMilliseconds,
                    dnsEx.GetType().FullName,
                    dnsEx.Message);

                _logger.LogError(
                    "[SMTP-DIAG] INFRASTRUCTURE REPORT:\n"                                             +
                    "  DNS Resolved   : NO\n"                                                          +
                    "  TCP Reachable  : UNKNOWN\n"                                                     +
                    "  TLS Started    : UNKNOWN\n"                                                     +
                    "  Auth Reached   : UNKNOWN\n"                                                     +
                    "  Failure Stage  : DNS\n"                                                         +
                    "  Recommendation : Verify SMTP_HOST and container DNS config.");

                throw new InvalidOperationException("Gagal Mengirim Email", dnsEx);
            }

            // ══════════════════════════════════════════════════════════════
            // TASK 4 — Detect IPv6 Preference
            // Logs which address family the OS resolver returns first,
            // which is what a plain hostname ConnectAsync will attempt.
            // ══════════════════════════════════════════════════════════════
            if (resolvedAddresses.Length > 0)
            {
                var firstAddr  = resolvedAddresses[0];
                var firstFamily = firstAddr.AddressFamily == AddressFamily.InterNetwork
                    ? "IPv4" : "IPv6";

                _logger.LogInformation(
                    "[SMTP-DIAG] ── IPv6 PREFERENCE DETECTION ────────────────────\n"       +
                    "  First address returned by DNS : {FirstAddr} ({PreferredFamily})\n"   +
                    "  OS resolver will prefer       : {PreferredFamily2}\n"                +
                    "  MailKit ConnectAsync(hostname) will attempt {PreferredFamily3} first.\n" +
                    "  If Railway blocks {PreferredFamily4}, the connection will time out.",
                    firstAddr, firstFamily, firstFamily, firstFamily, firstFamily);

                if (firstAddr.AddressFamily == AddressFamily.InterNetworkV6)
                    _logger.LogWarning(
                        "[SMTP-DIAG] IPv6 is preferred. Railway containers often lack outbound " +
                        "IPv6 routing. This is the most likely cause of the timeout.");
            }

            // ══════════════════════════════════════════════════════════════
            // TASK 2 & 3 — TCP Hostname Test + Per-IP Exhaustive Test
            //
            // We test EVERY resolved IP individually so we know exactly
            // which address families work on this Railway deployment.
            // The hostname test result is recorded separately so Task 6
            // can compare TcpClient vs MailKit behaviour.
            // ══════════════════════════════════════════════════════════════
            var tcpTimeout   = TimeSpan.FromSeconds(10);
            var ipResults    = new List<IpTcpResult>();

            // ── 2a. Hostname-level TCP test ──────────────────────────────
            _logger.LogInformation(
                "[SMTP-DIAG] ── TCP TEST (hostname) ──────────────────────────\n" +
                "  Target  : {Host}:{Port}\n"                                    +
                "  Timeout : {Sec} s   Time : {Time}",
                _settings.Host, _settings.Port,
                tcpTimeout.TotalSeconds,
                DateTime.UtcNow.ToString("O"));

            bool hostnameTcpOk = false;
            var  hostnameTcpSw = Stopwatch.StartNew();
            try
            {
                await TcpConnectWithTimeoutAsync(_settings.Host, _settings.Port, tcpTimeout);
                hostnameTcpSw.Stop();
                hostnameTcpOk = true;
                tcpReachable  = true;

                _logger.LogInformation(
                    "[SMTP-DIAG] TCP (hostname) SUCCESS in {Ms} ms",
                    hostnameTcpSw.ElapsedMilliseconds);
            }
            catch (Exception tcpHostEx)
            {
                hostnameTcpSw.Stop();
                var se = tcpHostEx as SocketException ?? tcpHostEx.InnerException as SocketException;
                _logger.LogWarning(
                    "[SMTP-DIAG] TCP (hostname) FAILED after {Ms} ms — " +
                    "{ExType} | SocketError: {SockErr} | {Msg}",
                    hostnameTcpSw.ElapsedMilliseconds,
                    tcpHostEx.GetType().Name,
                    se?.SocketErrorCode.ToString() ?? "(n/a)",
                    tcpHostEx.Message);
            }

            // ── 2b. Per-IP exhaustive test (Task 3) ─────────────────────
            // Test ALL resolved IPs regardless of hostname result.
            _logger.LogInformation(
                "[SMTP-DIAG] ── TCP TEST (per-IP, all addresses) ─────────────\n" +
                "  Testing {Count} address(es) individually...",
                resolvedAddresses.Length);

            foreach (var addr in resolvedAddresses)
            {
                var family = addr.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6";
                var ipSw   = Stopwatch.StartNew();
                try
                {
                    await TcpConnectToIpWithTimeoutAsync(addr, _settings.Port, tcpTimeout);
                    ipSw.Stop();

                    ipResults.Add(new IpTcpResult(addr, addr.AddressFamily,
                        true, ipSw.ElapsedMilliseconds, string.Empty, string.Empty));

                    _logger.LogInformation(
                        "[SMTP-DIAG] TCP {Family} {Address}:{Port}  →  SUCCESS ({Ms} ms)",
                        family, addr, _settings.Port, ipSw.ElapsedMilliseconds);
                }
                catch (Exception ipEx)
                {
                    ipSw.Stop();
                    var se     = ipEx as SocketException ?? ipEx.InnerException as SocketException;
                    var exType = ipEx.GetType().Name;
                    var exMsg  = ipEx.Message;

                    ipResults.Add(new IpTcpResult(addr, addr.AddressFamily,
                        false, ipSw.ElapsedMilliseconds, exType, exMsg));

                    _logger.LogWarning(
                        "[SMTP-DIAG] TCP {Family} {Address}:{Port}  →  FAILED ({Ms} ms) " +
                        "| {ExType} | SocketError: {SockErr} | {Msg}",
                        family, addr, _settings.Port, ipSw.ElapsedMilliseconds,
                        exType,
                        se?.SocketErrorCode.ToString() ?? "(n/a)",
                        exMsg);
                }
            }

            // ── Summary of per-IP results ────────────────────────────────
            var ipv4Results = ipResults.Where(r => r.Family == AddressFamily.InterNetwork).ToArray();
            var ipv6Results = ipResults.Where(r => r.Family == AddressFamily.InterNetworkV6).ToArray();
            bool anyIpv4Ok  = ipv4Results.Any(r => r.Success);
            bool anyIpv6Ok  = ipv6Results.Any(r => r.Success);

            // FIX: also treat the hostname test as proof of reachability.
            // Previously tcpReachable was only set to true inside the hostname
            // catch-success branch, but if DNS resolves to IPv6 first and IPv6
            // times out, the hostname test fails even though the same port is
            // reachable via IPv4.  The per-IP loop correctly records anyIpv4Ok=true
            // in that case, so we must promote tcpReachable here.
            // DiagnosticsController never had this gap because it evaluates
            // reachability purely from its PerIpResults list — it never relies on
            // a separate tcpReachable boolean that only the hostname path could set.
            if (!tcpReachable && hostnameTcpOk) tcpReachable = true;
            if (!tcpReachable && anyIpv4Ok)     tcpReachable = true;
            if (!tcpReachable && anyIpv6Ok)     tcpReachable = true;

            // ── Task 2 (requested): log the exact state of every reachability flag ──
            _logger.LogInformation(
                "[SMTP-DIAG] TCP per-IP summary:\n"                        +
                "  resolvedAddresses.Length : {AddrCount}\n"               +
                "  IPv4 addresses tested    : {V4Total}\n"                 +
                "  IPv4 success             : {V4Ok}\n"                    +
                "  IPv6 addresses tested    : {V6Total}\n"                 +
                "  IPv6 success             : {V6Ok}\n"                    +
                "  hostnameTcpOk            : {HostOk}\n"                  +
                "  anyIpv4Ok                : {AnyV4}\n"                   +
                "  anyIpv6Ok                : {AnyV6}\n"                   +
                "  tcpReachable (final)     : {TcpReachable}",
                resolvedAddresses.Length,
                ipv4Results.Length, anyIpv4Ok,
                ipv6Results.Length, anyIpv6Ok,
                hostnameTcpOk,
                anyIpv4Ok,
                anyIpv6Ok,
                tcpReachable);

            // ══════════════════════════════════════════════════════════════
            // TASK 6 — TcpClient vs MailKit comparison verdict
            //
            // If raw TcpClient succeeded but we expect MailKit to fail
            // because only non-preferred IPs work, report that here.
            // ══════════════════════════════════════════════════════════════
            if (tcpReachable && !hostnameTcpOk)
            {
                _logger.LogWarning(
                    "[SMTP-DIAG] ── TcpClient vs MailKit Verdict ─────────────────\n" +
                    "  TcpClient (per-IP)      : At least one IP succeeded\n"         +
                    "  TcpClient (hostname)    : FAILED\n"                             +
                    "  MailKit ConnectAsync    : Will use hostname → likely FAILS\n"  +
                    "  Conclusion              : INFRASTRUCTURE ISSUE\n"               +
                    "                            OS resolver prefers a non-working address family.\n" +
                    "                            MailKit cannot override resolver order without\n"      +
                    "                            explicit IP-address connection.");
            }
            else if (!tcpReachable)
            {
                _logger.LogError(
                    "[SMTP-DIAG] ── TcpClient vs MailKit Verdict ─────────────────\n" +
                    "  TcpClient (hostname)    : FAILED\n"                             +
                    "  TcpClient (per-IP)      : ALL addresses FAILED\n"               +
                    "  MailKit ConnectAsync    : Will FAIL\n"                          +
                    "  Conclusion              : INFRASTRUCTURE BLOCKING\n"             +
                    "                            Outbound port {Port} is blocked or unreachable.",
                    _settings.Port);
            }
            else
            {
                _logger.LogInformation(
                    "[SMTP-DIAG] ── TcpClient vs MailKit Verdict ─────────────────\n" +
                    "  TcpClient (hostname)    : OK\n"                                 +
                    "  MailKit ConnectAsync    : Expected to succeed\n"                +
                    "  If MailKit fails        : Config issue, not infrastructure.");
            }

            // ══════════════════════════════════════════════════════════════
            // TASK 8 — Scenario-based recommendation (before MailKit attempt)
            // ══════════════════════════════════════════════════════════════
            if (!tcpReachable)
            {
                // Scenario B: neither IPv4 nor IPv6 can connect
                _logger.LogError(
                    "[SMTP-DIAG] ── SCENARIO B: ALL TCP BLOCKED ──────────────────\n"                    +
                    "  Conclusion    : Outbound SMTP from this deployment is blocked entirely.\n"         +
                    "  Recommendations:\n"                                                                +
                    "    1. Enable outbound networking in Railway project settings.\n"                    +
                    "    2. Try SMTP port 465 instead of 587 (set SMTP_PORT=465).\n"                     +
                    "    3. Switch to an HTTP-based transactional email relay:\n"                         +
                    "         • Resend   — https://resend.com  (HTTP API, no raw SMTP)\n"                +
                    "         • SendGrid — https://sendgrid.com\n"                                       +
                    "         • Mailgun  — https://mailgun.com\n"                                        +
                    "    4. Do not replace Gmail in code — only change the infrastructure.\n"             +
                    "  Business Logic Changed : NO");

                throw new InvalidOperationException(
                    "Gagal Mengirim Email: outbound SMTP is blocked by the deployment environment.");
            }

            if (anyIpv4Ok && !anyIpv6Ok && !hostnameTcpOk)
            {
                // Scenario A: IPv4 succeeds, IPv6 fails, hostname fails
                _logger.LogWarning(
                    "[SMTP-DIAG] ── SCENARIO A: IPv4 OK, IPv6 BLOCKED ────────────\n"                  +
                    "  IPv4 TCP      : SUCCESS\n"                                                        +
                    "  IPv6 TCP      : FAILED\n"                                                         +
                    "  Hostname TCP  : FAILED (OS chose IPv6 first)\n"                                   +
                    "  Minimal Fix   : Connect MailKit directly to the working IPv4 address\n"          +
                    "                  instead of the hostname.\n"                                       +
                    "  Implementation: Pass the first reachable IPv4 address as the host\n"             +
                    "                  parameter to client.ConnectAsync(). No business logic changes.");
            }

            // ══════════════════════════════════════════════════════════════
            // Attachment / Message Size Logging
            // ══════════════════════════════════════════════════════════════
            _logger.LogInformation(
                "[SMTP-DIAG] ── MESSAGE CONSTRUCTION ─────────────────────────");

            var message     = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.User));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            int htmlBytes = System.Text.Encoding.UTF8.GetByteCount(htmlBody ?? string.Empty);
            _logger.LogInformation(
                "[SMTP-DIAG] HTML body : {Bytes} bytes ({Kb:F1} KB)",
                htmlBytes, htmlBytes / 1024.0);

            if (attachmentBytes != null && attachmentBytes.Length > 0
                && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                _logger.LogInformation(
                    "[SMTP-DIAG] Attachment: '{Name}' | {Bytes} bytes ({Kb:F1} KB)",
                    attachmentFileName, attachmentBytes.Length, attachmentBytes.Length / 1024.0);

                if (attachmentBytes.Length > 5 * 1024 * 1024)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: Attachment > 5 MB ({Mb:F2} MB). " +
                        "May cause SMTP timeout.",
                        attachmentBytes.Length / (1024.0 * 1024.0));

                bodyBuilder.Attachments.Add(
                    attachmentFileName,
                    attachmentBytes,
                    MimeKit.MimeTypes.GetMimeType(attachmentFileName) is string mt
                        ? ContentType.Parse(mt)
                        : ContentType.Parse("application/octet-stream"));
            }
            else
            {
                _logger.LogInformation("[SMTP-DIAG] Attachment: (none)");
            }

            message.Body = bodyBuilder.ToMessageBody();

            try
            {
                using var sizeStream = new MemoryStream();
                await message.WriteToAsync(sizeStream);
                long mimeBytes = sizeStream.Length;
                _logger.LogInformation(
                    "[SMTP-DIAG] MimeMessage total : {Bytes} bytes ({Kb:F1} KB / {Mb:F2} MB)",
                    mimeBytes, mimeBytes / 1024.0, mimeBytes / (1024.0 * 1024.0));
                if (mimeBytes > 10 * 1024 * 1024)
                    _logger.LogWarning(
                        "[SMTP-DIAG] WARNING: MimeMessage > 10 MB ({Mb:F2} MB). " +
                        "Likely to be rejected by Gmail SMTP.",
                        mimeBytes / (1024.0 * 1024.0));
            }
            catch (Exception sizeEx)
            {
                _logger.LogWarning(sizeEx,
                    "[SMTP-DIAG] Could not measure MimeMessage size: {Msg}", sizeEx.Message);
            }

            _logger.LogInformation("[SMTP-DIAG] MimeMessage built successfully.");

            // ══════════════════════════════════════════════════════════════
            // TASK 5 — SMTP Stage Timing
            // Determine the connection host: prefer a working IPv4 address
            // when the hostname TCP test failed but IPv4 succeeded (Scenario A).
            // ══════════════════════════════════════════════════════════════
            string smtpConnectHost = _settings.Host;
            if (!hostnameTcpOk && anyIpv4Ok)
            {
                var firstWorkingIpv4 = ipv4Results.First(r => r.Success).Address.ToString();
                smtpConnectHost = firstWorkingIpv4;
                _logger.LogInformation(
                    "[SMTP-DIAG] Scenario A fix applied: MailKit will connect to " +
                    "IPv4 address {Addr} instead of hostname {Host}.",
                    firstWorkingIpv4, _settings.Host);
            }

            // SmtpTimeoutSeconds is configurable via SMTP_TIMEOUT_SECONDS env var (default: 20 s).
            // Keeping it well below Railway's 60 s upstream timeout lets the controller
            // return a structured HTTP response before the proxy resets the HTTP/2 stream.
            int smtpTimeoutMs = Math.Max(5_000, _settings.SmtpTimeoutSeconds * 1_000);
            _logger.LogInformation(
                "[SMTP-DIAG] SmtpClient.Timeout = {TimeoutMs} ms ({TimeoutSec} s) " +
                "(configured via EmailSettings.SmtpTimeoutSeconds / SMTP_TIMEOUT_SECONDS)",
                smtpTimeoutMs, _settings.SmtpTimeoutSeconds);

            using var client = new SmtpClient { Timeout = smtpTimeoutMs };

            var overallSw  = Stopwatch.StartNew();
            var connectSw  = new Stopwatch();
            var authSw     = new Stopwatch();
            var sendSw     = new Stopwatch();

            try
            {
                // ── ConnectAsync ─────────────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── ConnectAsync ─────────────────────────────\n" +
                    "  Target   : {Host}:{Port}\n"                                +
                    "  TLS Mode : StartTls\n"                                     +
                    "  Time     : {Time}",
                    smtpConnectHost, _settings.Port,
                    DateTime.UtcNow.ToString("O"));

                connectSw.Start();
                await client.ConnectAsync(
                    smtpConnectHost, _settings.Port, SecureSocketOptions.StartTls);
                connectSw.Stop();
                tlsStarted = true;

                _logger.LogInformation(
                    "[SMTP-DIAG] ConnectAsync OK in {Ms} ms | " +
                    "Connected={Connected} Secure={Secure} | Time={Time}",
                    connectSw.ElapsedMilliseconds,
                    client.IsConnected, client.IsSecure,
                    DateTime.UtcNow.ToString("O"));

                // ── AuthenticateAsync ─────────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── AuthenticateAsync ───────────────────────\n" +
                    "  User : {User}   Time : {Time}",
                    _settings.User, DateTime.UtcNow.ToString("O"));

                authReached = true;
                authSw.Start();
                await client.AuthenticateAsync(_settings.User, _settings.AppPassword);
                authSw.Stop();

                _logger.LogInformation(
                    "[SMTP-DIAG] AuthenticateAsync OK in {Ms} ms | Time={Time}",
                    authSw.ElapsedMilliseconds, DateTime.UtcNow.ToString("O"));

                // ── SendAsync ─────────────────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── SendAsync ────────────────────────────────\n" +
                    "  To : {To}   Subject : {Subject}   Time : {Time}",
                    toEmail, subject, DateTime.UtcNow.ToString("O"));

                sendSw.Start();
                await client.SendAsync(message);
                sendSw.Stop();

                _logger.LogInformation(
                    "[SMTP-DIAG] SendAsync OK in {Ms} ms | Time={Time}",
                    sendSw.ElapsedMilliseconds, DateTime.UtcNow.ToString("O"));

                overallSw.Stop();

                // ── TASK 5 Timing Summary ─────────────────────────────────
                _logger.LogInformation(
                    "[SMTP-DIAG] ── TIMING SUMMARY ───────────────────────────\n" +
                    "  DNS lookup     : {Dns,6} ms\n"                            +
                    "  TCP hostname   : {TcpHost,6} ms  ({TcpHostResult})\n"    +
                    "  SMTP connect   : {Smtp,6} ms\n"                           +
                    "  Authenticate   : {Auth,6} ms\n"                           +
                    "  Send           : {Send,6} ms\n"                           +
                    "  ─────────────────────────────────────────\n"             +
                    "  Total elapsed  : {Total,6} ms",
                    dnsSw.ElapsedMilliseconds,
                    hostnameTcpSw.ElapsedMilliseconds,
                    hostnameTcpOk ? "OK" : "FAILED",
                    connectSw.ElapsedMilliseconds,
                    authSw.ElapsedMilliseconds,
                    sendSw.ElapsedMilliseconds,
                    overallSw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                overallSw.Stop();
                connectSw.Stop();
                authSw.Stop();
                sendSw.Stop();

                var se          = ex as SocketException ?? ex.InnerException as SocketException;
                var socketError = se?.SocketErrorCode.ToString() ?? "(n/a)";

                _logger.LogError(
                    ex,
                    "[SMTP-DIAG] SMTP FAILED\n"                              +
                    "  ExceptionType      : {ExType}\n"                      +
                    "  Message            : {Msg}\n"                         +
                    "  InnerExceptionType : {InnerType}\n"                   +
                    "  InnerMessage       : {InnerMsg}\n"                    +
                    "  SocketErrorCode    : {SockErr}\n"                     +
                    "  Elapsed            : {Ms} ms\n"                       +
                    "  StackTrace:\n{Stack}",
                    ex.GetType().FullName, ex.Message,
                    ex.InnerException?.GetType().FullName ?? "(none)",
                    ex.InnerException?.Message            ?? "(none)",
                    socketError,
                    overallSw.ElapsedMilliseconds,
                    ex.StackTrace);

                // ── TASK 6 comparison at failure point ───────────────────
                string tcpVsMailkitVerdict;
                if (!tcpReachable)
                    tcpVsMailkitVerdict =
                        "TcpClient FAILED + MailKit FAILED → INFRASTRUCTURE BLOCKING";
                else if (!hostnameTcpOk && tcpReachable)
                    tcpVsMailkitVerdict =
                        "TcpClient (per-IP) OK but hostname FAILED → INFRASTRUCTURE (IPv6 preference)";
                else
                    tcpVsMailkitVerdict =
                        "TcpClient OK + MailKit FAILED → MAILKIT CONFIGURATION ISSUE";

                // ── TASK 7 Infrastructure Report ─────────────────────────
                string failStage =
                    !tlsStarted  ? "MailKit ConnectAsync / TLS negotiation" :
                    !authReached ? "AuthenticateAsync"                      :
                                   "SendAsync";
                string issueType =
                    !tlsStarted  ? "Infrastructure (network / TLS)" :
                    !authReached ? "Credentials or Google account config" :
                                   "SMTP relay / message content";

                _logger.LogError(
                    "[SMTP-DIAG] ── INFRASTRUCTURE REPORT ───────────────────────\n"         +
                    "  DNS Resolved           : {DnsOk}\n"                                   +
                    "  Resolved IPs           : {IpCount} address(es)\n"                     +
                    "  IPv4 TCP reachable     : {V4Ok}\n"                                    +
                    "  IPv6 TCP reachable     : {V6Ok}\n"                                    +
                    "  Hostname TCP reachable : {HostOk}\n"                                  +
                    "  SMTP Greeting reached  : {SmtpGreeting}\n"                            +
                    "  TLS reached            : {TlsOk}\n"                                   +
                    "  Auth reached           : {AuthOk}\n"                                  +
                    "  Exact failure point    : {Stage}\n"                                   +
                    "  Issue type             : {IssueType}\n"                               +
                    "  TcpClient vs MailKit   : {Verdict}\n"                                 +
                    "  Railway networking     : {RailwayNote}\n"                             +
                    "  Application Logic OK   : YES — no business logic changed\n"           +
                    "  Elapsed                : {Ms} ms\n"                                   +
                    "  ─────────────────────────────────────────────────────────\n"          +
                    "  Recommendations:\n"                                                    +
                    "    • If TCP blocked    → Settings → Networking in Railway project.\n"   +
                    "    • If port 587 blocked → set SMTP_PORT=465 (SMTPS).\n"               +
                    "    • If IPv6 preferred → Scenario A fix already applied (IPv4 direct).\n" +
                    "    • If all TCP blocked → switch to HTTP relay (Resend/SendGrid/Mailgun).\n" +
                    "    • If Auth failed    → regenerate Gmail App Password (2-FA required).",
                    dnsResolved,
                    resolvedAddresses.Length,
                    anyIpv4Ok,
                    anyIpv6Ok,
                    hostnameTcpOk,
                    tlsStarted,
                    tlsStarted,
                    authReached,
                    failStage,
                    issueType,
                    tcpVsMailkitVerdict,
                    !tcpReachable
                        ? "Outbound SMTP appears blocked"
                        : "Outbound TCP reachable (at least one IP succeeded)",
                    overallSw.ElapsedMilliseconds);

                throw new InvalidOperationException("Gagal Mengirim Email", ex);
            }
            finally
            {
                // ── DisconnectAsync ───────────────────────────────────────
                if (client.IsConnected)
                {
                    _logger.LogInformation(
                        "[SMTP-DIAG] ── DisconnectAsync ──────────────────────────\n" +
                        "  Time : {Time}", DateTime.UtcNow.ToString("O"));

                    var dSw = Stopwatch.StartNew();
                    try
                    {
                        await client.DisconnectAsync(true);
                        dSw.Stop();
                        _logger.LogInformation(
                            "[SMTP-DIAG] DisconnectAsync OK in {Ms} ms | Time={Time}",
                            dSw.ElapsedMilliseconds, DateTime.UtcNow.ToString("O"));
                    }
                    catch (Exception dEx)
                    {
                        dSw.Stop();
                        _logger.LogWarning(dEx,
                            "[SMTP-DIAG] DisconnectAsync failed (non-critical): {Msg}",
                            dEx.Message);
                    }
                }
            }
        }
    }
}
