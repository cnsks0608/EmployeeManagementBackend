using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.Models;        
using EmployeeManagement.Api.Exceptions;

namespace EmployeeManagement.Api.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;  // tokendan id claimini okuyabilmek için 

        public UserService(AppDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        // yardımcı, private metod - token'daki NameIdentifier claim'inden, giriş yapmış kullanıcının Id'sini okur
        private int GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!.Value);
        }

        public async Task<UserAdminDto> GetMeAsync()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null) // mesela kullanıcı login durumundayken geçerli bir tokenı varken admin tarafından silinirse, userid null dönebilir (o yüzden burada exception attık)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var userDto = _mapper.Map<UserAdminDto>(user);
            return userDto;
        }

        public async Task<UserAdminDto> UpdateMeAsync(UpdateMeDto updateMeDto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            user.Username = updateMeDto.Username;
            user.Email = updateMeDto.Email;

            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserAdminDto>(user);
            return userDto;
        }

        public async Task DeleteMeAsync()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserAdminDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            var userDtos = _mapper.Map<List<UserAdminDto>>(users);
            return userDtos;
        }

        public async Task<UserAdminDto> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            var userDto = _mapper.Map<UserAdminDto>(user);
            return userDto;
        }

        public async Task<UserAdminDto> UpdateUserByAdminAsync(int id, UpdateUserByAdminDto updateUserByAdminDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            user.Username = updateUserByAdminDto.Username;
            user.Email = updateUserByAdminDto.Email;
            user.RoleId = (int)updateUserByAdminDto.RoleType;

            await _context.SaveChangesAsync();

            var userDto = _mapper.Map<UserAdminDto>(user);
            return userDto;
        }

        public async Task DeleteUserByAdminAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}