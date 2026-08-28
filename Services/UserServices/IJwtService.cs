using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services.UserServices
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}