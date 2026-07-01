using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Services;
using trinova_erp_backend.Usecase;

namespace trinova_erp_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin,admin")]
    public class MasterUserController : ControllerBase
    {
        private readonly IMasterUserUsecase _masterUser;
        private readonly IActivityLogService _activityLogService;

        public MasterUserController(
            IMasterUserUsecase masterUser,
            IActivityLogService activityLogService)
        {
            _masterUser = masterUser;
            _activityLogService = activityLogService;
        }

        [HttpPost("/api/auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginUser([FromBody] LoginRequest payload)
        {
            try
            {
                var result = await _masterUser.LoginUserAsync(payload);

                await _activityLogService.LogAsync(new ActivityLogCreate
                {
                    Module = "security",
                    ActivityType = "login_success",
                    Title = $"Login success: {result.user.Email}",
                    Description = $"{result.user.Username} logged in as {result.user.RoleName}.",
                    RefTable = "master_user",
                    RefId = result.user.Id,
                    RefNumber = result.user.Email
                });

                return Ok(new
                {
                    success = true,
                    message = "Login berhasil",
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                await _activityLogService.LogAsync(new ActivityLogCreate
                {
                    Module = "security",
                    ActivityType = "login_failed",
                    Title = $"Login failed: {payload.email}",
                    Description = ex.Message,
                    RefTable = "auth_login",
                    RefNumber = payload.email
                });

                return Unauthorized(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        [HttpGet("/api/user-list")]
        public async Task<IActionResult> GetAllUser()
        {
            try
            {
                var result = await _masterUser.GetAllUser();

                return Ok(new
                {
                    success = true,
                    message = "Data user berhasil diambil",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("/api/create-user")]
        public async Task<IActionResult> CreateUser([FromBody] MasterUserDTO request)
        {
            try
            {
                var result = await _masterUser.CreateUser(request);

                return Ok(new
                {
                    success = true,
                    message = "User berhasil dibuat",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPut("/api/update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] MasterUserDTO request)
        {
            try
            {
                var result = await _masterUser.UpdateUser(request);

                return Ok(new
                {
                    success = true,
                    message = "User berhasil diupdate",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("/api/role-list")]
        public async Task<IActionResult> GetAllRole()
        {
            try
            {
                var result = await _masterUser.GetAllRole();

                return Ok(new
                {
                    success = true,
                    message = "Data role berhasil diambil",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPut("/api/toggle-user-status/{id}")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            try
            {
                var result = await _masterUser.ToggleUserStatus(id);

                return Ok(new
                {
                    success = true,
                    message = "Status user berhasil diubah",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
