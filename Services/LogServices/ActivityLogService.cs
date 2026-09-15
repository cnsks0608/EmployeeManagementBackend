using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.DTOs.LogDtos;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Api.Services.LogServices
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string? _currentUsername;
        private readonly IMapper _mapper;


        public ActivityLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _currentUsername = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }


        public async Task LogActivityAsync(
            string? username,
            string? targetName,
            string action,
            string description,
            bool isSuccess,
            string? failureReason = null)
        {
            var log = new ActivityLog
            {
                Username = username,
                TargetName = targetName,
                Action = action,
                Description = description,
                IsSuccess = isSuccess,
                FailureReason = failureReason
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        // burası her işlemden sonra tek tek log kaydı oluşturmak yerine bunu merkezi bir şekilde yapmamızı sağlayan yardımcı fonksiyon

        public async Task<PagedResult<ActivityLogDto>> GetAllActivityLogsAsync(
            string? username,
            string? targetName,
            string? action,
            bool? isSuccess,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortDirection = null)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(username))
            {
                var lowerUsername = username.ToLower();
                query = query.Where(l => l.Username != null && l.Username.ToLower().Contains(lowerUsername));
            }

            if (!string.IsNullOrWhiteSpace(targetName))
            {
                var lowerTargetName = targetName.ToLower();
                query = query.Where(l => l.TargetName != null && l.TargetName.ToLower().Contains(lowerTargetName));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(l => l.Action.ToUpper() == action.ToUpper());
            }

            if (isSuccess.HasValue)
            {
                query = query.Where(l => l.IsSuccess == isSuccess.Value);
            }

        
            // EF Core ve PostgreSQL timestamptz uyumlu UTC dönüşümleri:
            if (startDate.HasValue)
            {
                var startDateTime = DateTime.SpecifyKind(startDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt >= startDateTime);
            }

            if (endDate.HasValue)
            {
                var endDateTime = DateTime.SpecifyKind(endDate.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt <= endDateTime);
            }


            query = sortDirection == "asc"
                  ? query.OrderBy(l => l.CreatedAt)
                  : query.OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync();  // sayfalamadan ÖNCE, filtrelere uyan TOPLAM kayıt sayısı
            var skip = (pageNumber - 1) * pageSize;
            query = query.Skip(skip).Take(pageSize);
            var logs = await query.ToListAsync();  // sadece o sayfadaki kayıtları çekiyoruz

            var logDtos = _mapper.Map<List<ActivityLogDto>>(logs);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);


            var appliedFilters = new List<string>();

            if (!string.IsNullOrWhiteSpace(username))
                appliedFilters.Add($"username: {username}");

            if (!string.IsNullOrWhiteSpace(targetName))
                appliedFilters.Add($"targetName: {targetName}");

            if (!string.IsNullOrWhiteSpace(action))
                appliedFilters.Add($"action: {action}");

            if (isSuccess.HasValue)
                appliedFilters.Add($"isSuccess: {isSuccess}");

            if (startDate.HasValue)
                appliedFilters.Add($"startDate: {startDate}");

            if (endDate.HasValue)
                appliedFilters.Add($"endDate: {endDate}");

            var filterDescription = appliedFilters.Count > 0
                ? $" (Filtreler: {string.Join(", ", appliedFilters)})"
                : "";


            var searchedTarget = string.IsNullOrWhiteSpace(targetName) && string.IsNullOrWhiteSpace(username)
              ? null : $"{username} {targetName}".Trim();

            await LogActivityAsync(
                username: _currentUsername,
                targetName: searchedTarget,
                action: "Read",
                description: $"{_currentUsername} adlı kullanıcı, aktivite loglarını görüntüledi. ({totalCount} kayıt bulundu){filterDescription}",
                isSuccess: true
            );

            return new PagedResult<ActivityLogDto>
            {
                Items = logDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
            // burada da ayrıca manuel bir şekilde requestlog logu oluşturmuyoruz çünkü middleware tarafından otomatik oluşturuluyor zaten
        }
    }
}