namespace trinova_erp_backend.Config
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public string FromName { get; set; } = "Trinova ERP";
        public int SmtpTimeoutSeconds { get; set; } = 20;
    }
}
