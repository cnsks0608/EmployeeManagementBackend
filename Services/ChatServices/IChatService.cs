using EmployeeManagement.Api.DTOs.ChatDtos;

namespace EmployeeManagement.Api.Services.ChatServices
{
    public interface IChatService
    {
        Task<ResponseDto> GetAnswerAsync(RequestDto requestDto, string? roleName);
    }
}