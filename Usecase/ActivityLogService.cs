using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using trinova_erp_backend.Models;
using trinova_erp_backend.Repositories;

namespace trinova_erp_backend.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(ActivityLogCreate data);
        Task LogSalesAsync(
            string activityType,
            string title,
            string? description,
            string refTable,
            long? refId,
            string? refNumber);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly IActivityLogRepo _activityLogRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            IActivityLogRepo activityLogRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _activityLogRepo = activityLogRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogSalesAsync(
            string activityType,
            string title,
            string? description,
            string refTable,
            long? refId,
            string? refNumber)
        {
            await LogAsync(new ActivityLogCreate
            {
                Module = "sales",
                ActivityType = activityType,
                Title = title,
                Description = description,
                RefTable = refTable,
                RefId = refId,
                RefNumber = refNumber
            });
        }

        public async Task LogAsync(ActivityLogCreate data)
        {
            if (string.IsNullOrWhiteSpace(data.Module)
                || string.IsNullOrWhiteSpace(data.ActivityType)
                || string.IsNullOrWhiteSpace(data.Title))
            {
                return;
            }

            var actor = ResolveActor();

            await _activityLogRepo.InsertAsync(new ActivityLog
            {
                Module = data.Module,
                ActivityType = data.ActivityType,
                Title = data.Title,
                Description = data.Description,
                RefTable = data.RefTable,
                RefId = data.RefId,
                RefNumber = data.RefNumber,
                UserId = actor.UserId,
                UserName = actor.UserName,
                IpAddress = ResolveIpAddress()
            });
        }

        private string? ResolveIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // Behind a reverse proxy/load balancer the real client IP is in
            // X-Forwarded-For (first entry); fall back to the socket IP.
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }

        private ActivityActor ResolveActor()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return new ActivityActor(0, "SYSTEM");

            var userIdValue =
                context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Request.Headers["X-User-Id"].FirstOrDefault();

            var userName =
                context.User.FindFirstValue(ClaimTypes.Name)
                ?? context.Request.Headers["X-User-Name"].FirstOrDefault()
                ?? "SYSTEM";

            return new ActivityActor(
                int.TryParse(userIdValue, out var userId) ? userId : 0,
                string.IsNullOrWhiteSpace(userName) ? "SYSTEM" : userName);
        }

        private sealed record ActivityActor(int UserId, string UserName);
    }
}