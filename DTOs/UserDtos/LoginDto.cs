namespace EmployeeManagement.Api.DTOs.UserDtos
{
    public class LoginDto
    {
        public string MailOrUsername { get; set; } = string.Empty;  
        public string Password { get; set; } = string.Empty;
    }
}