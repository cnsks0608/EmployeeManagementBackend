using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestLogController : ControllerBase
    {
        private readonly IRequestLogService _requestLogService;

        public RequestLogController(IRequestLogService requestLogService)
        {
            _requestLogService = requestLogService;
        }

        [HttpGet("GetAllRequestLogs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllRequestLogs(
            [FromQuery] string? httpMethod,
            [FromQuery] int? statusCode,
            [FromQuery] string? username,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var logs = await _requestLogService.GetAllRequestLogsAsync(httpMethod, statusCode, username, startDate, endDate);
            return Ok(logs);
        }
    }
}