using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.LogDtos;

namespace EmployeeManagement.Api.Services.LogServices
{
    public class RequestLogService : IRequestLogService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public RequestLogService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
            return logDtos;
        }
    }
}