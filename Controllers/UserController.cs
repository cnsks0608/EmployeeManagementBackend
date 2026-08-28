using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Api.Services.UserServices;
using EmployeeManagement.Api.DTOs.UserDtos;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Exceptions;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetMe")]
        [Authorize]

        public async Task<IActionResult> GetMe()
        {
            try
            {
                var user = await _userService.GetMeAsync();
                return Ok(user);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("UpdateMe")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UpdateMeDto updateMeDto)
        {
            try
            {
                var user = await _userService.UpdateMeAsync(updateMeDto);
                return Ok(new { message = "Bilgileriniz başarılı bir şekilde güncellendi.", user });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("DeleteMe")]
        [Authorize]
        public async Task<IActionResult> DeleteMe()
        {
            try
            {
                await _userService.DeleteMeAsync();
                return Ok(new { message = "Hesabınız başarılı bir şekilde silindi." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("GetAllUsers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("GetUserById/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(user);
            }

            catch (NotFoundException ex)      //  daha spesifik olan, önce kontrol edilir (sıralama önemli çünkü catchler yukarıdan aşağıya kontrol edilir)
            {
                return NotFound(ex.Message);   // 404
            }

            /* catch (Exception ex)
            {
                return BadRequest(ex.Message);  // 400 -> ama serviste notfound dışında bir exception fırlatmadığımız için bu bloğa herhangi bir şey gelmeyecek (öngörülemeyen hatalar middlewaree gidecek)
            } */
        }

        [HttpPut("UpdateUserByAdmin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserByAdmin(int id, UpdateUserByAdminDto updateUserByAdminDto)
        {
            try
            {
                var user = await _userService.UpdateUserByAdminAsync(id, updateUserByAdminDto);
                return Ok(new { message = "Kullanıcı başarılı bir şekilde güncellendi.", user });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("DeleteUserByAdmin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserByAdmin(int id)
        {
            try
            {
                await _userService.DeleteUserByAdminAsync(id);
                return Ok(new { message = "Kullanıcı başarılı bir şekilde silindi." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}