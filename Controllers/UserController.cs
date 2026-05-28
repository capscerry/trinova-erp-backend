using Microsoft.AspNetCore.Mvc;
using trinova_erp_backend.Models.DTO;
using trinova_erp_backend.Usecase;

namespace trinova_erp_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IMasterUserUsecase _masterUser;
        public UserController(IMasterUserUsecase masterUser)
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

        [HttpPost("/api/create-user")]
        public async Task<IActionResult> CreateUser([FromBody] MasterUserDTO request)  
        {
            try
            {
                var result = await _masterUser.CreateUser(request);
                return Ok(new
                {
                    success = true,
                    message = "User Berhasil Dibuat",
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
