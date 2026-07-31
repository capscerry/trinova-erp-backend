namespace trinova_erp_backend.Config
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string FromName { get; set; } = "Trinova ERP";

        /// <summary>
        /// MailKit SmtpClient.Timeout in seconds.
        /// Configurable via SMTP_TIMEOUT_SECONDS env var (default: 20 s).
        ///
        /// Keeping this below Railway's upstream timeout (60 s) ensures the
        /// application can return a structured HTTP response before the proxy
        /// resets the HTTP/2 stream, which would cause ERR_HTTP2_PROTOCOL_ERROR
        /// in the browser even though the backend error was caught correctly.
        ///
        /// Recommended values:
        ///   Development : 20 s  (fast feedback on local network issues)
        ///   Production  : 20 s  (leaves ample margin before Railway's 60 s limit)
        /// </summary>
        public int SmtpTimeoutSeconds { get; set; } = 20;
    }
}
