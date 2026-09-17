using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Api.Services.ChatServices;
using EmployeeManagement.Api.DTOs.ChatDtos;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("Ask")]
        [Authorize]
        public async Task<IActionResult> Ask(RequestDto requestDto)
        {
            var roleName = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;  // tokendan rolenamei okuyoruz 
            var response = await _chatService.GetAnswerAsync(requestDto, roleName);
            return Ok(response);
        }
    }
}