namespace trinova_erp_backend.Models
{
    public class ActivityLog
    {
        public long Id { get; set; }
        public string Module { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RefTable { get; set; }
        public long? RefId { get; set; }
        public string? RefNumber { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityLogCreate
    {
        public string Module { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RefTable { get; set; }
        public long? RefId { get; set; }
        public string? RefNumber { get; set; }
    }
}
