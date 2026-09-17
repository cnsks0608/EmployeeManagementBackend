namespace EmployeeManagement.Api.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public Role? Role { get; set; }
    }
}