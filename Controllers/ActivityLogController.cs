using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [HttpGet("GetAllActivityLogs")]
        public async Task<IActionResult> GetAllActivityLogs(
            [FromQuery] string? username,
            [FromQuery] string? targetName,
            [FromQuery] string? action,
            [FromQuery] bool? isSuccess,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var logs = await _activityLogService.GetAllActivityLogsAsync(username, targetName, action, isSuccess, startDate, endDate, pageNumber, pageSize);
            return Ok(logs);
        }
    }
}