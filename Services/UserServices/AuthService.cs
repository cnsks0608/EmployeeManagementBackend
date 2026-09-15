using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.Models;
using AutoMapper;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Services.UserServices
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;
        private readonly IActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string? _currentUsername;

        public AuthService(AppDbContext context, IJwtService jwtService, IMapper mapper, IActivityLogService activityLogService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _jwtService = jwtService;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
            _currentUsername = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        public async Task<UserAdminDto> CreateUserByAdminAsync(CreateUserByAdminDto createUserByAdminDto)
        {
            var employee = await _context.Employees.FindAsync(createUserByAdminDto.EmployeeId);

            if (employee == null || employee.RowStatus == RowStatus.Deleted)
            {
                throw new Exception("Seçilen çalışan bulunamadı veya silinmiş durumda.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u =>
        u.EmployeeId == createUserByAdminDto.EmployeeId);

            if (existingUser != null)
            {
                throw new Exception("Bu çalışana ait zaten bir kullanıcı hesabı bulunmaktadır.");
            }


            var passwordHash = BCrypt.Net.BCrypt.HashPassword(createUserByAdminDto.Password);  // dtodan gelen şifreyi hashler

            var newUser = new User
            {
                Username = createUserByAdminDto.Username,
                Email = createUserByAdminDto.Email,
                PasswordHash = passwordHash,  // şifre veriabnında hashlenmiş halde tutulur
                RoleId = (int)createUserByAdminDto.RoleType, // kayıt sayfasında role için enum aldık onu int e çevirip veritabanında roleid olarak sakladık
                EmployeeId = createUserByAdminDto.EmployeeId
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            await _context.Entry(newUser).Reference(u => u.Role).LoadAsync();  // veritabanına yeni kaydedilen newUser nesnesinin Role navigation property'sini ayrıca bir sorguyla dolduruyor. Sorgularda RoleName i de görebilmemiz için 

            var userDto = _mapper.Map<UserAdminDto>(newUser);

            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: newUser.Username,
              action: "Create",
              description: $"{_currentUsername}, {newUser.Username} adlı yeni bir kullanıcı hesabı oluşturdu.",
              isSuccess: true);

            return userDto;

        }

        public async Task<string> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == loginDto.MailOrUsername || u.Email == loginDto.MailOrUsername);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                    username: loginDto.MailOrUsername,
                    targetName: null,
                    action: "Login",
                    description: $"{loginDto.MailOrUsername} ile giriş yapmaya çalışıldı ancak kullanıcı bulunamadı.",
                    isSuccess: false);
                throw new Exception("Kullanıcı adı/email veya şifre hatalı.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                await _activityLogService.LogActivityAsync(
                    username: null,
                    targetName: loginDto.MailOrUsername,
                    action: "Login",
                    description: $"{loginDto.MailOrUsername} ile giriş yapmaya çalışıldı ancak şifre hatalı.",
                    isSuccess: false);
                throw new Exception("Kullanıcı adı/email veya şifre hatalı.");
            }

            if (user.RowStatus == RowStatus.Deleted)
            {
                await _activityLogService.LogActivityAsync(
                    username: loginDto.MailOrUsername,
                    targetName: loginDto.MailOrUsername,
                    action: "Login",
                    description: $"{loginDto.MailOrUsername} ile giriş yapmaya çalışıldı ancak kullanıcı silinmiş.",
                    isSuccess: false);
                throw new Exception("Kullanıcı adı/email veya şifre hatalı.");
            }

            var token = _jwtService.GenerateToken(user);
            await _activityLogService.LogActivityAsync(
                username: user.Username,
                targetName: user.Username,
                action: "Login",
                description: $"{user.Username} adlı kullanıcı başarılı bir şekilde giriş yaptı.",
                isSuccess: true);
            return token;
        }

        public async Task LogoutAsync()
        {
            await _activityLogService.LogActivityAsync(
                username: _currentUsername,
                targetName: _currentUsername,
                action: "Logout",
                description: $"{_currentUsername} adlı kullanıcı başarılı bir şekilde çıkış yaptı.",
                isSuccess: true);
        }

    }
}