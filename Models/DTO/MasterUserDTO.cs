namespace trinova_erp_backend.Models.DTO
{
    public class MasterUserDTO
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? Email { get; set; }
            
        public string? RoleName { get; set; }

        public int? RoleId { get; set; }
        public string? Password { get; set; }
        public bool Status { get; set; }

    }
}
