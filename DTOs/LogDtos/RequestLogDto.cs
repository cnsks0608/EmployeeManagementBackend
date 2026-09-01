namespace EmployeeManagement.Api.DTOs.LogDtos
{
    public class RequestLogDto
    {
        public int Id { get; set; }
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? Username { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}