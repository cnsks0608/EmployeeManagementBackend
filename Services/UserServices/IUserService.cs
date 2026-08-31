using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.DTOs;
namespace EmployeeManagement.Api.Services.UserServices
{
    public interface IUserService // sadece dışarıya açık metodları liseler, yardımcı metodları listelemez - getcurrentuserid gibi
    {
        Task<UserAdminDto> GetMeAsync();  // id yi parametre olarak vermeyip token ın id claiminden alacağız 
        Task<UserAdminDto> UpdateMeAsync(UpdateMeDto updateMeDto);
        Task DeleteMeAsync();

        Task<PagedResult<UserAdminDto>> GetAllUsersAsync(
            string? search,
            RoleType? role,
            int? employeeId,
            int pageNumber = 1,   // varsayılan: 1. sayfa (belirtilmezse otomatik bu değer kullanılır)
            int pageSize = 10,   // varsayılan: sayfa başına 10 kayıt
            string status = "active");
        Task<UserAdminDto> GetUserByIdAsync(int id);
        Task<UserAdminDto> UpdateUserByAdminAsync(int id, UpdateUserByAdminDto updateUserByAdminDto);
        Task DeleteUserByAdminAsync(int id);
        Task ChangePasswordAsync(ChangePasswordDto changePasswordDto);
        Task<UserAdminDto> ReactivateUserAsync(int id);
    }
}