using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;  // tokendan id claimini okuyabilmek için 
        private readonly IActivityLogService _activityLogService;
        private readonly string? _currentUsername;

        public UserService(AppDbContext context, IMapper mapper, IHttpContextAccessor httpContextAccessor, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _activityLogService = activityLogService;
            _currentUsername = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
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
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) // mesela kullanıcı login durumundayken geçerli bir tokenı varken admin tarafından silinirse, userid null dönebilir (o yüzden burada exception attık)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: _currentUsername,
                  action: "Read",
                  description: $"{_currentUsername} adlı kullanıcı, kendi bilgilerini görüntülemeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı");
            }

            var userDto = _mapper.Map<UserAdminDto>(user);
            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: _currentUsername,
              action: "Read",
              description: $"{_currentUsername} adlı kullanıcı, kendi bilgilerini görüntüledi.",
              isSuccess: true);

            return userDto;
        }

        public async Task<UserAdminDto> UpdateMeAsync(UpdateMeDto updateMeDto)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: _currentUsername,
                  action: "Update",
                  description: $"{_currentUsername} adlı kullanıcı, kendi bilgilerini güncellemeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı");
            }
            var changes = new List<string>();

            if (user.Username != updateMeDto.Username)
                changes.Add($"Username: {user.Username} → {updateMeDto.Username}");

            // Değişiklik kontrollerini alanları değişirmeden önce yapmalıyız yoksa değişiklik yokmuş gibi olur

            user.Username = updateMeDto.Username;
            user.RowStatus = RowStatus.Updated;

            await _context.SaveChangesAsync();

            var changeDescription = changes.Count > 0 ? string.Join(", ", changes) : "herhangi bir değişiklik yapılmadı";

            await _activityLogService.LogActivityAsync(
                username: _currentUsername,
                targetName: _currentUsername,
                action: "Update",
                description: $"{_currentUsername} adlı kullanıcı, kendi bilgilerini güncelledi: {changeDescription}",
                isSuccess: true);

            var userDto = _mapper.Map<UserAdminDto>(user);
            return userDto;
        }

        public async Task DeleteMeAsync()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: _currentUsername,
                  action: "Delete",
                  description: $"{_currentUsername} adlı kullanıcı, kendi hesabını silmeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            user.RowStatus = RowStatus.Deleted;
            await _context.SaveChangesAsync();
            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: _currentUsername,
              action: "Delete",
              description: $"{_currentUsername} adlı kullanıcı, kendi hesabını sildi.",
              isSuccess: true);
        }

        public async Task<PagedResult<UserAdminDto>> GetAllUsersAsync(
            string? search,
            RoleType? role,
            int? employeeId,
            int pageNumber = 1,
            int pageSize = 10,
            string status = "active",
            string? sortDirection = null)
        {
            var query = _context.Users.Include(u => u.Role).Include(u => u.Employee).AsQueryable();

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

            if (status == "active")
            {
                query = query.Where(u => u.RowStatus != RowStatus.Deleted);
            }
            else if (status == "deleted")
            {
                query = query.Where(u => u.RowStatus == RowStatus.Deleted);
            }

            query = sortDirection == "desc"
                ? query.OrderByDescending(u => u.Username)
                : query.OrderBy(u => u.Username);

            var totalCount = await query.CountAsync();  // filtrelere uyan TOPLAM kayıt sayısı (sayfalama uygulanmadan ÖNCE sayılmalı eğer sonra yapsaydık sadece o sayfadaki count sayısı gelirdi)
            var skip = (pageNumber - 1) * pageSize;  // kaç kaydın atlanacağını hesaplıyoruz
            query = query.Skip(skip).Take(pageSize);  // ilgili sayfanın kayıtlarını kesiyoruz
            var users = await query.ToListAsync();  // sadece o sayfadaki kayıtları veritabanından çekiyoruz
            var userDtos = _mapper.Map<List<UserAdminDto>>(users);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var appliedFilters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
                appliedFilters.Add($"search: {search}");

            if (role.HasValue)
                appliedFilters.Add($"role: {role}");

            if (employeeId.HasValue)
                appliedFilters.Add($"employeeId: {employeeId}");

            var filterDescription = appliedFilters.Count > 0
                ? $" (Filtreler: {string.Join(", ", appliedFilters)})"
                : "";

            await _activityLogService.LogActivityAsync(
                username: _currentUsername,
                targetName: null,
                action: "Read",
                description: $"{_currentUsername} adlı kullanıcı, kullanıcı listesini görüntüledi. ({totalCount} kayıt bulundu){filterDescription}",
                isSuccess: true);


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
            var user = await _context.Users.Include(u => u.Role).Include(u => u.Employee).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: null,
                  action: "Read",
                  description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan kullanıcıyı görüntülemeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            var userDto = _mapper.Map<UserAdminDto>(user);
            await _activityLogService.LogActivityAsync(
               username: _currentUsername,
               targetName: user.Username,
               action: "Read",
               description: $"{_currentUsername} adlı kullanıcı, {user.Username} adlı kullanıcının bilgilerini görüntüledi.",
               isSuccess: true);

            return userDto;
        }

        public async Task<UserAdminDto> UpdateUserByAdminAsync(int id, UpdateUserByAdminDto updateUserByAdminDto)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: null,
                  action: "Update",
                  description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan kullanıcıyı güncellemeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            var changes = new List<string>();

            if (user.Username != updateUserByAdminDto.Username)
                changes.Add($"Username: {user.Username} → {updateUserByAdminDto.Username}");


            if (user.RoleId != (int)updateUserByAdminDto.RoleType)
                changes.Add($"Role Id: {user.RoleId} → {(int)updateUserByAdminDto.RoleType}");


            user.Username = updateUserByAdminDto.Username;
            user.RoleId = (int)updateUserByAdminDto.RoleType;
            user.RowStatus = RowStatus.Updated;

            await _context.SaveChangesAsync();
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();  // RoleId değişmiş olabileceği için Role'ü güncel haliyle tekrar yüklüyoruz

            var userDto = _mapper.Map<UserAdminDto>(user);

            var changeDescription = changes.Count > 0 ? string.Join(", ", changes) : "herhangi bir değişiklik yapılmadı";

            await _activityLogService.LogActivityAsync(
                username: _currentUsername,
                targetName: user.Username,
                action: "Update",
                description: $"{_currentUsername} adlı kullanıcı, {user.Username} adlı kullanıcının bilgilerini güncelledi: {changeDescription}",
                isSuccess: true);

            return userDto;
        }

        public async Task DeleteUserByAdminAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: null,
                  action: "Delete",
                  description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan kullanıcıyı silmeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            user.RowStatus = RowStatus.Deleted;
            await _context.SaveChangesAsync();
            await _activityLogService.LogActivityAsync(
               username: _currentUsername,
               targetName: user.Username,
               action: "Delete",
               description: $"{_currentUsername} adlı kullanıcı, {user.Username} adlı kullanıcıyı sildi.",
               isSuccess: true);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto changePasswordDto)
        {
            var userId = GetCurrentUserId();  // giriş yapmış kullanıcının id'sini token'dan alıyoruz
            var user = await _context.Users.FindAsync(userId);

            if (user == null)  // savunma amaçlı kontrol, GetMeAsync'deki gibi
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: _currentUsername,
                  action: "Update",
                  description: $"{_currentUsername} adlı kullanıcı, kendi şifresini değiştirmeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash);  // girilen mevcut şifre, veritabanındaki hashlenmiş şifreyle eşleşiyor mu kontrol ediyoruz

            if (!isCurrentPasswordValid)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: _currentUsername,
                  action: "Update",
                  description: $"{_currentUsername} adlı kullanıcı, kendi şifresini değiştirmeye çalıştı ama mevcut şifresi hatalıydı.",
                  isSuccess: false,
                  failureReason: "Mevcut şifre hatalı.");
                throw new Exception("Mevcut şifreniz hatalı.");  // NotFoundException değil, genel Exception - çünkü bu 400 olarak kalmalı
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);  // yeni şifreyi hashleyip kaydediyoruz

            await _context.SaveChangesAsync();

            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: _currentUsername,
              action: "Update",
              description: $"{_currentUsername} adlı kullanıcı, kendi şifresini başarıyla değiştirdi.",
              isSuccess: true);
        }

        public async Task<UserAdminDto> ReactivateUserAsync(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: null,
                  action: "Reactivate",
                  description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan kullanıcıyı yeniden aktifleştirmeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir kullanıcı bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir kullanıcı bulunamadı.");
            }

            if (user.RowStatus != RowStatus.Deleted)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: user.Username,
                  action: "Reactivate",
                  description: $"{_currentUsername} adlı kullanıcı, {user.Username} adlı kullanıcıyı yeniden aktifleştirmeye çalıştı ama zaten aktifti.",
                  isSuccess: false,
                  failureReason: "Bu kullanıcı zaten aktif.");
                throw new Exception("Bu kullanıcı zaten aktif.");
            }

            var linkedEmployee = await _context.Employees.FindAsync(user.EmployeeId);
            if (linkedEmployee == null ||linkedEmployee.RowStatus == RowStatus.Deleted)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: user.Username,
                  action: "Reactivate",
                  description: $"{_currentUsername} adlı kullanıcı, {user.Username} adlı kullanıcıyı yeniden aktifleştirmeye çalıştı ama bağlı olduğu çalışan silinmiş durumda.",
                  isSuccess: false,
                  failureReason: "Bağlı çalışan silinmiş durumda, önce çalışanın tekrar aktif edilmesi gerekiyor.");
                throw new Exception("Bu kullanıcının bağlı olduğu çalışan silinmiş durumda. Önce çalışanı tekrar aktif etmelisiniz.");
            }

            user.RowStatus = RowStatus.Updated;

            await _context.SaveChangesAsync();

            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: user.Username,
              action: "Reactivate",
              description: $"{_currentUsername} adlı kullanıcı, {user.Username} adlı kullanıcıyı yeniden aktifleştirdi.",
              isSuccess: true);

            var userDto = _mapper.Map<UserAdminDto>(user);
            return userDto;
        }
    }
}