using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RequestLogController : ControllerBase
    {
        private readonly IRequestLogService _requestLogService;

        public RequestLogController(IRequestLogService requestLogService)
        {
            _requestLogService = requestLogService;
        }

        [HttpGet("GetAllRequestLogs")]
        public async Task<IActionResult> GetAllRequestLogs(
            [FromQuery] string? httpMethod,
            [FromQuery] int? statusCode,
            [FromQuery] string? username,
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortDirection = null)
        {
            var logs = await _requestLogService.GetAllRequestLogsAsync(httpMethod, statusCode, username, startDate, endDate, pageNumber, pageSize, sortDirection);
            return Ok(logs);
        }
    }
}