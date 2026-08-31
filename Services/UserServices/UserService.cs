using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.DTOs;

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

        public async Task<PagedResult<UserAdminDto>> GetAllUsersAsync(
            string? search,
            RoleType? role,
            int? employeeId,
            int pageNumber = 1,
            int pageSize = 10)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(e =>
                    e.Email.ToLower().Contains(lowerSearch) || e.Username.ToLower().Contains(lowerSearch));
            }

            if (role.HasValue)
            {
                int roleId = (int)role.Value;
                query = query.Where(u => u.RoleId == roleId);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(u => u.EmployeeId == employeeId.Value);
            }

            var totalCount = await query.CountAsync();  // filtrelere uyan TOPLAM kayıt sayısı (sayfalama uygulanmadan ÖNCE sayılmalı eğer sonra yapsaydık sadece o sayfadaki count sayısı gelirdi)
            var skip = (pageNumber - 1) * pageSize;  // kaç kaydın atlanacağını hesaplıyoruz
            query = query.Skip(skip).Take(pageSize);  // ilgili sayfanın kayıtlarını kesiyoruz
            var users = await query.ToListAsync();  // sadece o sayfadaki kayıtları veritabanından çekiyoruz
            var userDtos = _mapper.Map<List<UserAdminDto>>(users);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<UserAdminDto>
            {
                Items = userDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
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

        public async Task ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        {
            var userId = GetCurrentUserId();  // giriş yapmış kullanıcının id'sini token'dan alıyoruz
            var user = await _context.Users.FindAsync(userId);

            if (user == null)  // savunma amaçlı kontrol, GetMeAsync'deki gibi
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash);  // girilen mevcut şifre, veritabanındaki hashlenmiş şifreyle eşleşiyor mu kontrol ediyoruz

            if (!isCurrentPasswordValid)
            {
                throw new Exception("Mevcut şifreniz hatalı.");  // NotFoundException değil, genel Exception - çünkü bu 400 olarak kalmalı
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);  // yeni şifreyi hashleyip kaydediyoruz

            await _context.SaveChangesAsync();
        }
    }
}