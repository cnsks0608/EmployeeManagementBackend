using EmployeeManagement.Api.DTOs.LogDtos;

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

        Task<List<ActivityLogDto>> GetAllActivityLogsAsync(
            string? username,
            string? targetName,
            string? action,
            bool? isSuccess,
            DateTime? startDate,
            DateTime? endDate);
           
    }
}