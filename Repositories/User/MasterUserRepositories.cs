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
        Task<MasterUserDTO> CreateUser(MasterUserDTO user);
        Task<List<MasterRoleDTO>> GetAllRole();
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

        public async Task<MasterUserDTO> CreateUser(MasterUserDTO user)
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
                    OUTPUT 
                        INSERTED.id,
                        INSERTED.username,
                        INSERTED.email,
                        (SELECT role_name 
                         FROM master_role 
                         WHERE id = INSERTED.role_id) AS RoleName,
                        INSERTED.is_active AS Status
                    VALUES
                    (
                        @Username,
                        @Email,
                        @Password,
                        (SELECT id FROM master_role WHERE role_name = @RoleName),
                        @Status
                    );
                ";

            var result = await connection.QueryFirstOrDefaultAsync<MasterUserDTO>(
                query,
                new
                {
                    user.Username,
                    user.Email,
                    user.Password,
                    user.RoleName,
                    user.Status
                }
            );

            return result!;
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
