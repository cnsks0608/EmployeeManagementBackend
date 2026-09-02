using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Services.UserServices;
using EmployeeManagement.Api.DTOs.UserDtos;
using FluentValidation;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<CreateUserByAdminDto> _createUserValidator;
        private readonly IActivityLogService _logService;

        public AuthController(IAuthService authService, IValidator<CreateUserByAdminDto> createUserValidator, IActivityLogService logService)
        {
            _authService = authService;
            _createUserValidator = createUserValidator;
            _logService = logService;
        }

        [HttpPost("CreateUserByAdmin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUserByAdmin(CreateUserByAdminDto createUserByAdminDto)
        {
            var validationResult = await _createUserValidator.ValidateAsync(createUserByAdminDto);

            if (!validationResult.IsValid)
            {
                await _logService.LogActivityAsync(
                    username: User.Identity?.Name,
                    targetName: null,
                    action: "Create",
                    description: $"{User.Identity?.Name} adlı kullanıcı, yeni bir kullanıcı oluşturmaya çalıştı ama girdiği bilgiler geçersizdi.",
                    isSuccess: false,
                    failureReason: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors);
            }

            try
            {
                var user = await _authService.CreateUserByAdminAsync(createUserByAdminDto);
                return Ok(new { message = "Kullanıcı başarılı bir şekilde oluşturuldu.", user });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Login")]
        
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            try
            {
                var token = await _authService.LoginAsync(loginDto);
                return Ok(new { message = "Giriş başarılı.", token });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _authService.LogoutAsync();
                return Ok(new { message = "Çıkış başarılı." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}