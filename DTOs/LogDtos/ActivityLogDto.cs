namespace EmployeeManagement.Api.DTOs.LogDtos
{
    public class ActivityLogDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? TargetName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}