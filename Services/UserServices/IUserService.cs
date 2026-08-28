using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Services.UserServices
{
    public interface IUserService // sadece dışarıya açık metodları liseler, yardımcı metodları listelemez - getcurrentuserid gibi
    {
        Task<UserAdminDto> GetMeAsync();  // id yi parametre olarak vermeyip token ın id claiminden alacağız 
        Task<UserAdminDto> UpdateMeAsync(UpdateMeDto updateMeDto);
        Task DeleteMeAsync();

        Task<List<UserAdminDto>> GetAllUsersAsync();
        Task<UserAdminDto> GetUserByIdAsync(int id);
        Task<UserAdminDto> UpdateUserByAdminAsync(int id, UpdateUserByAdminDto updateUserByAdminDto);
        Task DeleteUserByAdminAsync(int id);
    }
}