namespace trinova_erp_backend.Models.DTO
{
    public class LoginResponse
    {
        public string? token { get; set; }
        public LoginUserResponse user { get; set; }
        
    }

    public class LoginUserResponse
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;
    }
}
