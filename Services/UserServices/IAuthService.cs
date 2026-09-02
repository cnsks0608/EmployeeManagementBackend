using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Services.UserServices
{
    public interface IAuthService
    {
        Task<UserAdminDto> CreateUserByAdminAsync(CreateUserByAdminDto createUserByAdminDto);
        Task<string> LoginAsync(LoginDto loginDto);
        Task LogoutAsync();
    }
}