using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.Models;
using AutoMapper;
using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Services.UserServices
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;

        public AuthService(AppDbContext context, IJwtService jwtService, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _jwtService = jwtService;
        }

        public async Task<UserAdminDto> CreateUserByAdminAsync(CreateUserByAdminDto createUserByAdminDto)
        {
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

            var userDto = _mapper.Map<UserAdminDto>(newUser);
            return userDto;

        }

        public async Task<string> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == loginDto.MailOrUsername || u.Email == loginDto.MailOrUsername);

            if (user == null)
            {
                throw new Exception("Kullanıcı adı/email veya şifre hatalı.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                throw new Exception("Kullanıcı adı/email veya şifre hatalı.");
            }

            if (user.RowStatus == RowStatus.Deleted)
            {
                throw new Exception("Kullanıcı adı/email veya şifre hatalı.");
            }

            var token = _jwtService.GenerateToken(user);
            return token;
        }

    }
}