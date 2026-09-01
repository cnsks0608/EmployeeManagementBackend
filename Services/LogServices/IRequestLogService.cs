using EmployeeManagement.Api.DTOs.LogDtos;

namespace EmployeeManagement.Api.Services.LogServices
{
    public interface IRequestLogService
    {
        Task<List<RequestLogDto>> GetAllRequestLogsAsync(
            string? httpMethod, 
            int? statusCode,
            string? username,
            DateTime? startDate,
            DateTime? endDate);
    }
}