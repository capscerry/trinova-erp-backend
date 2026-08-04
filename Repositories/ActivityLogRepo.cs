using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using trinova_erp_backend.Config;
using trinova_erp_backend.Models;

namespace trinova_erp_backend.Repositories
{
    public interface IActivityLogRepo
    {
        Task InsertAsync(ActivityLog log);
        Task<IEnumerable<ActivityLog>> GetSecurityAlertsAsync(int take = 12);
        Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 20);
    }

    public class ActivityLogRepo : IActivityLogRepo
    {
        private readonly string _connectionString;

        public ActivityLogRepo(IOptions<DatabaseConnection> options)
        {
            _connectionString = options.Value.SQLServer!;
        }

        public async Task InsertAsync(ActivityLog log)
        {
            const string query = @"
                INSERT INTO ActivityLogs
                (
                    Module,
                    ActivityType,
                    Title,
                    Description,
                    RefTable,
                    RefId,
                    RefNumber,
                    UserId,
                    UserName,
                    IpAddress
                )
                VALUES
                (
                    @Module,
                    @ActivityType,
                    @Title,
                    @Description,
                    @RefTable,
                    @RefId,
                    @RefNumber,
                    @UserId,
                    @UserName,
                    @IpAddress
                );";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query, log);
        }

        public async Task<IEnumerable<ActivityLog>> GetSecurityAlertsAsync(int take = 12)
        {
            const string query = @"
                SELECT TOP (@Take)
                    Id,
                    Module,
                    ActivityType,
                    Title,
                    Description,
                    RefTable,
                    RefId,
                    RefNumber,
                    UserId,
                    UserName,
                    IpAddress,
                    CreatedAt
                FROM ActivityLogs
                WHERE Module = 'security'
                  AND ActivityType IN (
                      'login_failed',
                      'unauthorized_access',
                      'authentication_required'
                  )
                ORDER BY CreatedAt DESC;";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ActivityLog>(query, new { Take = take });
        }

        // Dashboard "Aktivitas Terkini" timeline -- across every module (Purchasing,
        // Sales, Inventory, etc.), not scoped like GetSecurityAlertsAsync above.
        // Always excludes Module='security' -- login/auth/upload-rejected noise
        // belongs in the separate Security Alert panel (GetSecurityAlertsAsync),
        // not mixed into the business activity feed.
        public async Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 20)
        {
            const string query = @"
                SELECT TOP (@Take)
                    Id,
                    Module,
                    ActivityType,
                    Title,
                    Description,
                    RefTable,
                    RefId,
                    RefNumber,
                    UserId,
                    UserName,
                    IpAddress,
                    CreatedAt
                FROM ActivityLogs
                WHERE Module <> 'security'
                ORDER BY CreatedAt DESC;";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ActivityLog>(query, new { Take = take });
        }
    }
}