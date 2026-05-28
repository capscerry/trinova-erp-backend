using BCrypt.Net;
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
    }

    public class MasterUserUsecase : IMasterUserUsecase
    {
        private readonly IMasterUserRepositories _masterUserRepositories;

        public MasterUserUsecase(IMasterUserRepositories masterUserRepositories)
        {
            _masterUserRepositories = masterUserRepositories;
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
    }
}