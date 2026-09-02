using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Api.Services.UserServices;
using EmployeeManagement.Api.DTOs.UserDtos;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.DTOs;
using FluentValidation;
using EmployeeManagement.Api.Validators;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
        private readonly IActivityLogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UpdateMeValidator _updateMeValidator;
        private readonly UpdateUserByAdminValidator _updateUserByAdminValidator;

        public UserController(IUserService userService, IValidator<ChangePasswordDto> changePasswordValidator, IActivityLogService logService, IHttpContextAccessor httpContextAccessor, UpdateMeValidator updateMeValidator, UpdateUserByAdminValidator updateUserByAdminValidator)
        {
            _userService = userService;
            _changePasswordValidator = changePasswordValidator;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
            _updateMeValidator = updateMeValidator;
            _updateUserByAdminValidator = updateUserByAdminValidator;
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
            var validationResult = await _updateMeValidator.ValidateAsync(updateMeDto);
            if (!validationResult.IsValid)
            {
                await _logService.LogActivityAsync(
                    username: User.Identity?.Name,
                    targetName: User.Identity?.Name,
                    action: "Update",
                    description: $"{User.Identity?.Name} adlı kullanıcı, bilgilerini güncellemeye çalıştı ama girdiği bilgiler geçersizdi.",
                    isSuccess: false,
                    failureReason: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors);
            }
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
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? search,
            [FromQuery] RoleType? role,
            [FromQuery] int? employeeId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string status = "active")
        {
            var users = await _userService.GetAllUsersAsync(search, role, employeeId, pageNumber, pageSize, status);
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
            var validationResult = await _updateUserByAdminValidator.ValidateAsync(updateUserByAdminDto);
            if (!validationResult.IsValid)
            {
                await _logService.LogActivityAsync(
                    username: User.Identity?.Name,
                    targetName: updateUserByAdminDto.Username, // bu kısım da tam tutarlı değil ama hala veritabanına erişim istemiyoruz
                    action: "UpdateUserByAdmin",
                    description: $"{User.Identity?.Name} adlı Admin, {updateUserByAdminDto.Username} adlı kullanıcıyı güncellemeye çalıştı ama girdiği bilgiler geçersizdi.",
                    isSuccess: false,
                    failureReason: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors);}
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

        [HttpPut("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var validationResult = await _changePasswordValidator.ValidateAsync(changePasswordDto);
            if (!validationResult.IsValid)
            {
                await _logService.LogActivityAsync(
                    username: User.Identity?.Name,
                    targetName: User.Identity?.Name,
                    action: "Update",
                    description: $"{User.Identity?.Name} adlı kullanıcı, şifresini değiştirmeye çalıştı ama girdiği bilgiler geçersizdi.",
                    isSuccess: false,
                    failureReason: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors);
            }

            try
            {
                await _userService.ChangePasswordAsync(changePasswordDto);
                return Ok(new { message = "Şifreniz başarılı bir şekilde güncellendi." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("ReactivateUser/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            try
            {
                var user = await _userService.ReactivateUserAsync(id);
                return Ok(new { message = "Kullanıcı başarılı bir şekilde tekrar aktif edildi.", user });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


    }
}