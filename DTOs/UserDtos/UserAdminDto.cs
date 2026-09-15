using EmployeeManagement.Api.Enums;
namespace EmployeeManagement.Api.DTOs.UserDtos
{
    public class UserAdminDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeRegistrationNumber { get; set; } = string.Empty;
        public RowStatus RowStatus { get; set; }

    }
}