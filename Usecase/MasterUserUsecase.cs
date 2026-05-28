using BCrypt.Net;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Repositories.User;

namespace trinova_erp_backend.Usecase
{
    public interface IMasterUserUsecase
    {
        Task<List<MasterUserDTO>> GetAllUser();
        Task<MasterUserDTO> CreateUser(MasterUserDTO user);


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

        public async Task<MasterUserDTO> CreateUser(MasterUserDTO user)
        {

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                throw new Exception("Username wajib diisi");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new Exception("Email wajib diisi");
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                throw new Exception("Password wajib diisi");
            }

            if (string.IsNullOrWhiteSpace(user.RoleName))
            {
                throw new Exception("Role wajib dipilih");
            }

     
     
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            var result = await _masterUserRepositories.CreateUser(user);

            return result;
        }

        public async Task<List<MasterRoleDTO>> GetAllRole()
        {
            return await _masterUserRepositories.GetAllRole();

        }
    }
}
