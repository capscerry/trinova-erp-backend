using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Usecase;

namespace trinova_erp_backend.Controllers
{
    [ApiController]
    public class MasterUserController : ControllerBase
    {
        private readonly IMasterUserUsecase _masterUser;

        public MasterUserController(IMasterUserUsecase masterUser)
        {
            _masterUser = masterUser;
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