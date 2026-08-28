using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services.UserServices
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var roleType = (RoleType)user.RoleId;  // roleid sayısını roletype enumuna çeviriyoruz (çünkü controllerda [Authorize(Roles="Admin)] gibi kontrol edicez role bilgisi bize string olarak lazım)

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleType.ToString()), // burada enumu stringe çevirip tokenın claimine string olarak veriyoruz
                new Claim("EmployeeId", user.EmployeeId.ToString())  // tokena employeeid claimini de ekledik çünkü bir kullanıcı giriş yaptığında getemployeebyid fonksiyonunu çağırdığında maaş kısmını da gösetrmek istiyoruz. Bunun için login olmuş kullanıcının hangi employee ye denk geldiğini bilmemiz lazım 
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expireMinutes = Convert.ToDouble(_configuration["Jwt:ExpireMinutes"]);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}