namespace EmployeeManagement.Api.DTOs.CompanyDtos
{
    public class TitleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
    }
}