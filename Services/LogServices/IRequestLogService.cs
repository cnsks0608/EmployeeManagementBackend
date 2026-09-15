using EmployeeManagement.Api.DTOs.LogDtos;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Api.Services.LogServices
{
    public interface IRequestLogService
    {
        Task<PagedResult<RequestLogDto>> GetAllRequestLogsAsync(
            string? httpMethod, 
            int? statusCode,
            string? username,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortDirection = null);
    }
}