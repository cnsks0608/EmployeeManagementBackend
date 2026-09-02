using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.LogDtos;
using EmployeeManagement.Api.Services.LogServices;

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
      
        public async Task<List<RequestLogDto>> GetAllRequestLogsAsync(
            string? httpMethod,
            int? statusCode,
            string? username,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.RequestLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(httpMethod))
            {
                query = query.Where(l => l.HttpMethod == httpMethod.ToUpper());  // GET, POST gibi hep büyük harfle tutulduğu için büyük harfe çeviriyoruz
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

            if (startDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.CreatedAt <= endDate.Value);
            }

            var logs = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();  // en yeni logları en üstte göstermek için varsayılan olarak tarihe göre azalan sıralıyoruz
            var logDtos = _mapper.Map<List<RequestLogDto>>(logs);

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
                description: $"{_currentUsername} adlı kullanıcı, HTTP isteklerinin loglarını görüntüledi. ({logDtos.Count} kayıt bulundu){filterDescription}",
                isSuccess: true
            );


            // bu fonksiyonda activitylog daki gibi yeni bir requestlog logu eklemiyoruz çünkü middleware zaten otomatik ekliyor ancak bu fonksiyonu activity loga manuel ekliyoruz 

            return logDtos;
        }
    }
}