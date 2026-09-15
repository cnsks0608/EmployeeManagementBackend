using EmployeeManagement.Api.DTOs.LogDtos;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Api.Services.LogServices
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(
            string? username,
            string? targetName,
            string action,
            string description,
            bool isSuccess,
            string? failureReason = null);

        Task<PagedResult<ActivityLogDto>> GetAllActivityLogsAsync(
            string? username,
            string? targetName,
            string? action,
            bool? isSuccess,
            DateOnly? startDate,
            DateOnly? endDate,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortDirection = null);

    }
}