using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Services.UserServices;
using EmployeeManagement.Api.DTOs.UserDtos;
using FluentValidation;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<CreateUserByAdminDto> _createUserValidator;

        public AuthController(IAuthService authService, IValidator<CreateUserByAdminDto> createUserValidator)
        {
            _authService = authService;
            _createUserValidator = createUserValidator;
        }

        [HttpPost("CreateUserByAdmin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUserByAdmin(CreateUserByAdminDto createUserByAdminDto)
        {
            var validationResult = await _createUserValidator.ValidateAsync(createUserByAdminDto);

            if (!validationResult.IsValid)
            {
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
    }
}