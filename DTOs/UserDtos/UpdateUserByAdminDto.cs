using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.DTOs.UserDtos
{
    public class UpdateUserByAdminDto
    {
        public string Username { get; set; } = string.Empty;
        public RoleType RoleType { get; set; } //updatemedto dan farkı
    }
}