using BCrypt.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Repositories.User;

namespace trinova_erp_backend.Usecase
{
    public interface IMasterUserUsecase
    {
        Task<List<MasterUserDTO>> GetAllUser();
        Task<bool> CreateUser(MasterUserDTO user);
        Task<bool> UpdateUser(MasterUserDTO user);

        Task<bool> ToggleUserStatus(int id);

        Task<List<MasterRoleDTO>> GetAllRole();

        Task<LoginResponse> LoginUserAsync(LoginRequest data);
    }

    public class MasterUserUsecase : IMasterUserUsecase
    {
        private readonly IMasterUserRepositories _masterUserRepositories;
        private readonly JwtSettings _jwtSettings;
        private readonly IMemoryCache _memoryCache;
        private const int MaxFailedLoginAttempts = 5;
        private static readonly TimeSpan FailedLoginWindow = TimeSpan.FromMinutes(10);

        public MasterUserUsecase(
            IMasterUserRepositories masterUserRepositories,
            IOptions<JwtSettings> jwtSettings,
            IMemoryCache memoryCache)
        {
            _masterUserRepositories = masterUserRepositories;
            _jwtSettings = jwtSettings.Value;
            _memoryCache = memoryCache;
        }

        public async Task<List<MasterUserDTO>> GetAllUser()
        {
            return await _masterUserRepositories.GetAllUser();
        }

        public async Task<bool> CreateUser(MasterUserDTO user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new Exception("Username wajib diisi");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception("Email wajib diisi");

            if (string.IsNullOrWhiteSpace(user.Password))
                throw new Exception("Password wajib diisi");

            ValidatePasswordPolicy(user.Password);

            if (user.RoleId <= 0)
                throw new Exception("Role wajib dipilih");

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            return await _masterUserRepositories.CreateUser(user);
        }

        public async Task<bool> UpdateUser(MasterUserDTO user)
        {
            if (user.Id <= 0)
                throw new Exception("Id user tidak valid");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception("Email wajib diisi");

            if (user.RoleId <= 0)
                throw new Exception("Role wajib dipilih");

            return await _masterUserRepositories.UpdateUser(user);
        }

        public async Task<bool> ToggleUserStatus(int id)
        {
            if (id <= 0)
            {
                throw new Exception("Id user tidak valid");
            }

            return await _masterUserRepositories.ToggleUserStatus(id);
        }

        public async Task<List<MasterRoleDTO>> GetAllRole()
        {
            return await _masterUserRepositories.GetAllRole();
        }

        public async Task<LoginResponse> LoginUserAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.email) || string.IsNullOrWhiteSpace(request.password))
                throw new UnauthorizedAccessException("Email dan password wajib diisi");

            var loginKey = GetLoginAttemptKey(request.email);
            var failedAttempts = _memoryCache.Get<int>(loginKey);
            if (failedAttempts >= MaxFailedLoginAttempts)
                throw new UnauthorizedAccessException("Terlalu banyak percobaan login gagal. Coba lagi dalam 10 menit.");

            // get user by email 
            var userData = await _masterUserRepositories.GetUserByEmail(request.email);

            if (userData == null || userData.Id == 0)
            {
                RegisterFailedLogin(request.email);
                throw new UnauthorizedAccessException("Email atau Password salah "); 
            }


            // check if email and password is exist 
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.password, userData.Password);

            if (!isPasswordValid)
            {
                RegisterFailedLogin(request.email);
                throw new UnauthorizedAccessException("Password Salah");
            }

            if (!userData.Status)
                throw new UnauthorizedAccessException("Akun Anda telah dinonaktifkan. Hubungi administrator.");

            _memoryCache.Remove(loginKey);
            var token = GenerateJwtToken(userData);

            return new LoginResponse {
                   token = token,
                   user = new LoginUserResponse {
                        Id = userData.Id,
                        Username = userData.Username,
                        Email = userData.Email,
                        RoleName = userData.RoleName
                   }
            };
        }


        private void RegisterFailedLogin(string? email)
        {
            var loginKey = GetLoginAttemptKey(email);
            var failedAttempts = _memoryCache.Get<int>(loginKey) + 1;

            _memoryCache.Set(
                loginKey,
                failedAttempts,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = FailedLoginWindow
                });
        }

        private static string GetLoginAttemptKey(string? email)
        {
            return $"login-attempt:{(email ?? string.Empty).Trim().ToLowerInvariant()}";
        }

        private static void ValidatePasswordPolicy(string password)
        {
            if (password.Length < 8)
                throw new Exception("Password minimal 8 karakter");

            if (!password.Any(char.IsUpper))
                throw new Exception("Password harus memiliki minimal 1 huruf besar");

            if (!password.Any(char.IsLower))
                throw new Exception("Password harus memiliki minimal 1 huruf kecil");

            if (!password.Any(char.IsDigit))
                throw new Exception("Password harus memiliki minimal 1 angka");
        }


        private string GenerateJwtToken(MasterUserDTO masterUserDTO)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, masterUserDTO.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, masterUserDTO.Email),
            new Claim(ClaimTypes.Name, masterUserDTO.Username),
            new Claim(ClaimTypes.Role, masterUserDTO.RoleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Secret)
            );

            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
