using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.DTOs.LogDtos;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

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

        public async Task<List<ActivityLogDto>> GetAllActivityLogsAsync(
            string? username,
            string? targetName,
            string? action,
            bool? isSuccess,
            DateTime? startDate,
            DateTime? endDate)
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

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endDate.Value);
            }

            var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();  // en yeni logları en üstte göstermek için varsayılan olarak tarihe göre azalan sıralıyoruz


            var logDtos = _mapper.Map<List<ActivityLogDto>>(logs);

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
                description: $"{_currentUsername} adlı kullanıcı, aktivite loglarını görüntüledi. ({logDtos.Count} kayıt bulundu){filterDescription}",
                isSuccess: true
            );

            return logDtos;
            // burada da ayrıca manuel bir şekilde requestlog logu oluşturmuyoruz çünkü middleware tarafından otomatik oluşturuluyor zaten
        }
    }
}