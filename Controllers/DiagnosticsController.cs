using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;

namespace trinova_erp_backend.Controllers
{
    /// <summary>
    /// Pure diagnostic controller — no business logic.
    ///
    /// Task 6 (Prompt): Verify whether the deployed environment can establish
    /// a raw TCP connection to smtp.gmail.com:587 using TcpClient before MailKit
    /// is even involved.  If raw TCP also times out, the problem is the deployment
    /// network, not application code.
    ///
    /// GET  /api/diagnostics/smtp-connectivity
    ///   — Auth-free in Development, requires authenticated user in Production
    ///     so the endpoint cannot be abused as a port-scanner on Railway.
    ///
    /// GET  /api/diagnostics/env-check
    ///   — Returns presence (not value) of required env vars.
    ///     Safe to call in any environment; secrets are never echoed.
    /// </summary>
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly EmailSettings         _emailSettings;
        private readonly IWebHostEnvironment   _env;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            IOptions<EmailSettings>              emailSettings,
            IWebHostEnvironment                  env,
            ILogger<DiagnosticsController>       logger)
        {
            _emailSettings = emailSettings.Value;
            _env           = env;
            _logger        = logger;
        }

        // ── /api/diagnostics/smtp-connectivity ───────────────────────────────
        // Task 6: raw TcpClient probe (no MailKit) + DNS resolution details.
        // Requires [Authorize] in Production so it cannot be called anonymously
        // on the live Railway deployment.
        [HttpGet("/api/diagnostics/smtp-connectivity")]
        [AllowAnonymous]   // overridden by the filter below for Production
        public async Task<IActionResult> SmtpConnectivity(
            [FromQuery] string? host    = null,
            [FromQuery] int?    port    = null,
            [FromQuery] int     timeout = 10)
        {
            // ── Restrict to authenticated callers in Production ───────────────
            if (_env.IsProduction() && !User.Identity?.IsAuthenticated == true)
                return Unauthorized(new { status = false, message =
                    "This diagnostic endpoint requires authentication in Production." });

            var targetHost = host ?? _emailSettings.Host;
            var targetPort = port ?? _emailSettings.Port;
            var tcpTimeout = TimeSpan.FromSeconds(Math.Clamp(timeout, 2, 30));

            _logger.LogInformation(
                "[DIAG-SMTP] Connectivity probe started — Target={Host}:{Port} Timeout={Sec}s",
                targetHost, targetPort, tcpTimeout.TotalSeconds);

            var result = new SmtpConnectivityResult
            {
                TargetHost    = targetHost,
                TargetPort    = targetPort,
                TimeoutSeconds = (int)tcpTimeout.TotalSeconds,
                ProbeTime     = DateTime.UtcNow
            };

            // ── 1. DNS ────────────────────────────────────────────────────────
            var dnsSw = Stopwatch.StartNew();
            try
            {
                var addresses       = await Dns.GetHostAddressesAsync(targetHost);
                dnsSw.Stop();
                result.DnsElapsedMs = dnsSw.ElapsedMilliseconds;
                result.DnsSuccess   = true;
                result.ResolvedAddresses = addresses
                    .Select(a => new ResolvedAddress
                    {
                        Address = a.ToString(),
                        Family  = a.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6"
                    }).ToList();

                _logger.LogInformation(
                    "[DIAG-SMTP] DNS OK in {Ms}ms — {Count} address(es): {Addrs}",
                    dnsSw.ElapsedMilliseconds, addresses.Length,
                    string.Join(", ", addresses.Select(a => a.ToString())));

                // ── 2. TCP hostname ───────────────────────────────────────────
                var hostTcpSw = Stopwatch.StartNew();
                try
                {
                    using var tc = new TcpClient();
                    var connect  = tc.ConnectAsync(targetHost, targetPort);
                    if (await Task.WhenAny(connect, Task.Delay(tcpTimeout)) == connect)
                    {
                        await connect;   // surface SocketException if any
                        hostTcpSw.Stop();
                        result.TcpHostnameSuccess   = true;
                        result.TcpHostnameElapsedMs = hostTcpSw.ElapsedMilliseconds;
                        _logger.LogInformation(
                            "[DIAG-SMTP] TCP (hostname) OK in {Ms}ms", hostTcpSw.ElapsedMilliseconds);
                    }
                    else
                    {
                        hostTcpSw.Stop();
                        result.TcpHostnameSuccess   = false;
                        result.TcpHostnameElapsedMs = hostTcpSw.ElapsedMilliseconds;
                        result.TcpHostnameError     = $"Timed out after {tcpTimeout.TotalSeconds}s";
                        _logger.LogWarning(
                            "[DIAG-SMTP] TCP (hostname) TIMED OUT after {Ms}ms", hostTcpSw.ElapsedMilliseconds);
                    }
                }
                catch (Exception tcpEx)
                {
                    hostTcpSw.Stop();
                    result.TcpHostnameSuccess   = false;
                    result.TcpHostnameElapsedMs = hostTcpSw.ElapsedMilliseconds;
                    result.TcpHostnameError     = $"{tcpEx.GetType().Name}: {tcpEx.Message}";
                    _logger.LogWarning("[DIAG-SMTP] TCP (hostname) FAILED: {Err}", tcpEx.Message);
                }

                // ── 3. Per-IP TCP ─────────────────────────────────────────────
                foreach (var addr in addresses)
                {
                    var family  = addr.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6";
                    var ipEntry = new IpTcpProbeResult { Address = addr.ToString(), Family = family };
                    var ipSw    = Stopwatch.StartNew();
                    try
                    {
                        using var tc  = new TcpClient();
                        var connect   = tc.ConnectAsync(addr, targetPort);
                        if (await Task.WhenAny(connect, Task.Delay(tcpTimeout)) == connect)
                        {
                            await connect;
                            ipSw.Stop();
                            ipEntry.Success   = true;
                            ipEntry.ElapsedMs = ipSw.ElapsedMilliseconds;
                            _logger.LogInformation(
                                "[DIAG-SMTP] TCP {Family} {Addr}:{Port} OK in {Ms}ms",
                                family, addr, targetPort, ipSw.ElapsedMilliseconds);
                        }
                        else
                        {
                            ipSw.Stop();
                            ipEntry.Success   = false;
                            ipEntry.ElapsedMs = ipSw.ElapsedMilliseconds;
                            ipEntry.Error     = $"Timed out after {tcpTimeout.TotalSeconds}s";
                            _logger.LogWarning(
                                "[DIAG-SMTP] TCP {Family} {Addr}:{Port} TIMED OUT after {Ms}ms",
                                family, addr, targetPort, ipSw.ElapsedMilliseconds);
                        }
                    }
                    catch (Exception ipEx)
                    {
                        ipSw.Stop();
                        var se = ipEx as SocketException ?? ipEx.InnerException as SocketException;
                        ipEntry.Success      = false;
                        ipEntry.ElapsedMs    = ipSw.ElapsedMilliseconds;
                        ipEntry.Error        = $"{ipEx.GetType().Name}: {ipEx.Message}";
                        ipEntry.SocketError  = se?.SocketErrorCode.ToString();
                        _logger.LogWarning(
                            "[DIAG-SMTP] TCP {Family} {Addr}:{Port} FAILED {Ms}ms — {Err}",
                            family, addr, targetPort, ipSw.ElapsedMilliseconds, ipEx.Message);
                    }
                    result.PerIpResults.Add(ipEntry);
                }
            }
            catch (Exception dnsEx)
            {
                dnsSw.Stop();
                result.DnsElapsedMs = dnsSw.ElapsedMilliseconds;
                result.DnsSuccess   = false;
                result.DnsError     = $"{dnsEx.GetType().Name}: {dnsEx.Message}";
                _logger.LogError("[DIAG-SMTP] DNS FAILED: {Err}", dnsEx.Message);
            }

            // ── 4. Verdict ────────────────────────────────────────────────────
            bool anyIpv4Ok = result.PerIpResults.Any(r => r.Family == "IPv4" && r.Success);
            bool anyIpv6Ok = result.PerIpResults.Any(r => r.Family == "IPv6" && r.Success);

            result.Verdict = (!result.DnsSuccess)
                ? "DNS_FAILURE — Cannot resolve SMTP host. Check SMTP_HOST and container DNS."

                : (!result.TcpHostnameSuccess && !anyIpv4Ok && !anyIpv6Ok)
                ? "ALL_BLOCKED — Outbound SMTP is fully blocked. " +
                  "Enable networking in Railway or switch to an HTTP relay (Resend/SendGrid/Mailgun)."

                : (!result.TcpHostnameSuccess && anyIpv4Ok && !anyIpv6Ok)
                ? "IPv4_OK_IPv6_BLOCKED — OS resolver prefers IPv6 which is blocked. " +
                  "MailKit will fail via hostname. Scenario A fix in EmailService already routes " +
                  "MailKit to the working IPv4 address directly."

                : (!result.TcpHostnameSuccess && !anyIpv4Ok && anyIpv6Ok)
                ? "IPv6_OK_IPv4_BLOCKED — IPv6 works but hostname TCP failed. " +
                  "Adjust MailKit to connect to the working IPv6 address."

                : (result.TcpHostnameSuccess)
                ? "HOSTNAME_REACHABLE — Raw TCP to SMTP host works. " +
                  "If MailKit still fails, the issue is TLS negotiation or credentials, not networking."

                : "PARTIAL — Some IPs reachable. Review per-IP results.";

            result.SmtpTimeoutConfiguredSeconds = _emailSettings.SmtpTimeoutSeconds;
            result.Recommendation = BuildRecommendation(result, anyIpv4Ok, anyIpv6Ok);

            _logger.LogInformation(
                "[DIAG-SMTP] Probe complete — Verdict: {Verdict}", result.Verdict);

            return Ok(result);
        }

        // ── /api/diagnostics/env-check ────────────────────────────────────────
        // Returns which required env vars are present (never their values).
        [HttpGet("/api/diagnostics/env-check")]
        [Authorize]
        public IActionResult EnvCheck()
        {
            static bool Has(string key) =>
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key));

            return Ok(new
            {
                status = true,
                smtp = new
                {
                    SMTP_HOST            = Has("SMTP_HOST"),
                    SMTP_PORT            = Has("SMTP_PORT"),
                    SMTP_USER            = Has("SMTP_USER"),
                    SMTP_APP_PASSWORD    = Has("SMTP_APP_PASSWORD"),
                    SMTP_FROM_NAME       = Has("SMTP_FROM_NAME"),
                    SMTP_TIMEOUT_SECONDS = Has("SMTP_TIMEOUT_SECONDS"),
                    configured_host      = _emailSettings.Host,
                    configured_port      = _emailSettings.Port,
                    configured_timeout_s = _emailSettings.SmtpTimeoutSeconds,
                },
                runtime = new
                {
                    ASPNETCORE_ENVIRONMENT = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(not set)",
                    PORT                   = Environment.GetEnvironmentVariable("PORT") ?? "(not set)",
                    DOTNET_VERSION         = Environment.Version.ToString()
                }
            });
        }

        // ── helpers ───────────────────────────────────────────────────────────
        private static string BuildRecommendation(
            SmtpConnectivityResult r, bool anyIpv4Ok, bool anyIpv6Ok)
        {
            if (!r.DnsSuccess)
                return "Fix DNS: verify SMTP_HOST is set correctly and the container has DNS access.";

            if (!r.TcpHostnameSuccess && !anyIpv4Ok && !anyIpv6Ok)
                return "Outbound port " + r.TargetPort + " is completely blocked. " +
                       "Options: (1) Enable outbound networking in Railway project settings. " +
                       "(2) Try SMTP_PORT=465. " +
                       "(3) Switch to an HTTP-based relay: Resend, SendGrid, or Mailgun. " +
                       "Business logic does not need to change — only SMTP_HOST/SMTP_PORT env vars.";

            if (!r.TcpHostnameSuccess && anyIpv4Ok)
                return "IPv6 is preferred by the OS resolver but Railway blocks IPv6 outbound. " +
                       "The EmailService Scenario A fix already routes MailKit to the working " +
                       "IPv4 address automatically. No further code change is needed. " +
                       "Alternatively, set SMTP_HOST to the raw IPv4 address as a permanent fix.";

            if (r.TcpHostnameSuccess)
                return "Raw TCP works. If MailKit still throws TimeoutException, increase " +
                       "SMTP_TIMEOUT_SECONDS (currently " + r.SmtpTimeoutConfiguredSeconds + "s). " +
                       "If it throws AuthenticationException, regenerate the Gmail App Password.";

            return "Review per-IP results for the specific failing address families.";
        }
    }

    // ── Response models (diagnostic only, not part of ERP domain) ────────────
    public sealed class SmtpConnectivityResult
    {
        public string       TargetHost                   { get; set; } = string.Empty;
        public int          TargetPort                   { get; set; }
        public int          TimeoutSeconds               { get; set; }
        public DateTime     ProbeTime                    { get; set; }
        public bool         DnsSuccess                   { get; set; }
        public long         DnsElapsedMs                 { get; set; }
        public string?      DnsError                     { get; set; }
        public List<ResolvedAddress> ResolvedAddresses   { get; set; } = new();
        public bool         TcpHostnameSuccess           { get; set; }
        public long         TcpHostnameElapsedMs         { get; set; }
        public string?      TcpHostnameError             { get; set; }
        public List<IpTcpProbeResult> PerIpResults       { get; set; } = new();
        public string       Verdict                      { get; set; } = string.Empty;
        public string       Recommendation               { get; set; } = string.Empty;
        public int          SmtpTimeoutConfiguredSeconds { get; set; }
    }

    public sealed class ResolvedAddress
    {
        public string Address { get; set; } = string.Empty;
        public string Family  { get; set; } = string.Empty;
    }

    public sealed class IpTcpProbeResult
    {
        public string  Address     { get; set; } = string.Empty;
        public string  Family      { get; set; } = string.Empty;
        public bool    Success     { get; set; }
        public long    ElapsedMs   { get; set; }
        public string? Error       { get; set; }
        public string? SocketError { get; set; }
    }
}
