using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.DTOs.UserDtos
{
    public class CreateUserByAdminDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public RoleType RoleType { get; set; }

        // yeni user oluşturulurken id yi veritabanı otomatik atayacak o yüzden bu dto da yok

    }
}