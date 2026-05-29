using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models.DTO;

namespace trinova_erp_backend.Repositories.User
{
    public interface IMasterUserRepositories
    {
        Task<List<MasterUserDTO>> GetAllUser();
        Task<bool> CreateUser(MasterUserDTO user);
        Task<bool> UpdateUser(MasterUserDTO dataUpdate);

        Task<MasterUserDTO> GetUserByEmail(string email);

        
        Task<List<MasterRoleDTO>> GetAllRole();
        Task<bool> ToggleUserStatus(int id);
    }
    public class MasterUserRepositories : IMasterUserRepositories
    {
        private readonly string _connectionString;
        public MasterUserRepositories(IOptionsSnapshot<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }
        // =========================================================
        // GET ALL MASTER USER
        // =========================================================
        public async Task<List<MasterUserDTO>> GetAllUser()
        {
            using var connection = new SqlConnection(_connectionString);
            string query = @"SELECT 
                            mu.id AS Id,
                            mu.username AS Username,
                            mu.email As Email,
                            mr.role_name AS RoleName,
                            mu.is_active AS Status
                            FROM master_user mu JOIN master_role mr on mu.role_id = mr.id";

            var result = await connection.QueryAsync<MasterUserDTO>(query);
            return result.ToList();
        }

        public async Task<MasterUserDTO> GetUserByEmail(string name)
        {
            string query  = @"SELECT 
                                mu.id AS Id,
                                mu.username AS Username,
                                mu.email As Email,
                                mr.role_name AS RoleName,
                                mr.id As RoleId,   
                                mu.password As Password,
                                mu.is_active AS Status
                            FROM master_user mu JOIN master_role mr on mu.role_id = mr.id 
                            WHERE mu.email = @Email";
            using var connection = new SqlConnection(_connectionString);

            var result = await connection.QueryFirstOrDefaultAsync<MasterUserDTO>(
                    query,
                    new
                    {
                        Email = name
                    }
                );

            return result;
        }

        public async Task<bool> UpdateUser(MasterUserDTO dataUpdate)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                UPDATE master_user
                SET 
                    email = @Email,
                    role_id = @RoleId
                WHERE id = @Id
            ";

            var result = await connection.ExecuteAsync(
                query,
                new
                {
                    dataUpdate.Id,
                    dataUpdate.Email,
                    dataUpdate.RoleId
                }
            );

            return result > 0;
        }

        public async Task<bool> CreateUser(MasterUserDTO user)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                INSERT INTO master_user 
                (
                    username,
                    email,
                    password,   
                    role_id,
                    is_active
                )
                VALUES
                (
                    @Username,
                    @Email,
                    @Password,
                    @RoleId,
                    @Status
                );
            ";

            var result = await connection.ExecuteAsync(
                query,
                new
                {
                    user.Username,
                    user.Email,
                    user.Password,
                    user.RoleId,
                    user.Status
                }
            );

            return result > 0;
        }

        public async Task<bool> ToggleUserStatus(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string query = @"
                UPDATE master_user
                SET is_active = 
                    CASE 
                        WHEN is_active = 1 THEN 0
                        ELSE 1
                    END
                WHERE id = @Id
            ";

            var result = await connection.ExecuteAsync(
                query,
                new
                {
                    Id = id
                }
            );

            return result > 0;
        }

        // =========================================================
        // GET ALL MASTER ROLE
        // =========================================================
        public async Task<List<MasterRoleDTO>> GetAllRole()
        {
            using var connection = new SqlConnection(_connectionString);
            string query = @"SELECT 
                            id AS Id,
                            role_name AS Name
                            FROM master_role";
            var result = await connection.QueryAsync<MasterRoleDTO>(query);
            return result.ToList();
        }
    }
}
