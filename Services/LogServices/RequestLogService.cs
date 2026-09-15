using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.LogDtos;
using EmployeeManagement.Api.Services.LogServices;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Api.Services.LogServices
{
    public class RequestLogService : IRequestLogService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;
        private readonly string? _currentUsername;

        public RequestLogService(AppDbContext context, IMapper mapper, IActivityLogService activityLogService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
            _currentUsername = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        public async Task<PagedResult<RequestLogDto>> GetAllRequestLogsAsync(
            string? httpMethod,
            int? statusCode,
            string? username,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortDirection = null)
        {
            var query = _context.RequestLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(httpMethod))
            {
                query = query.Where(l => l.HttpMethod == httpMethod.ToUpper());
            }

            if (statusCode.HasValue)
            {
                query = query.Where(l => l.StatusCode == statusCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                var lowerUsername = username.ToLower();
                query = query.Where(l => l.Username != null && l.Username.ToLower().Contains(lowerUsername));
            }

            // EF Core uyumlu DateOnly dönüşümleri (l.CreatedAt.Date kullanılır):
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

            var totalCount = await query.CountAsync();
            var skip = (pageNumber - 1) * pageSize;
            query = query.Skip(skip).Take(pageSize);
            var logs = await query.ToListAsync();

            var logDtos = _mapper.Map<List<RequestLogDto>>(logs);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Log açıklaması için filtre yazıları hazırlanıyor:
            var appliedFilters = new List<string>();
            if (!string.IsNullOrWhiteSpace(httpMethod))
                appliedFilters.Add($"httpMethod: {httpMethod}");

            if (statusCode.HasValue)
                appliedFilters.Add($"statusCode: {statusCode}");

            if (!string.IsNullOrWhiteSpace(username))
                appliedFilters.Add($"username: {username}");

            if (startDate.HasValue)
                appliedFilters.Add($"startDate: {startDate}");

            if (endDate.HasValue)
                appliedFilters.Add($"endDate: {endDate}");

            var filterDescription = appliedFilters.Count > 0
                ? $" (Filtreler: {string.Join(", ", appliedFilters)})"
                : "";

            await _activityLogService.LogActivityAsync(
                username: _currentUsername,
                targetName: username,
                action: "Read",
                description: $"{_currentUsername} adlı kullanıcı, HTTP isteklerinin loglarını görüntüledi. ({totalCount} kayıt bulundu){filterDescription}",
                isSuccess: true
            );

            return new PagedResult<RequestLogDto>
            {
                Items = logDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
    }
}