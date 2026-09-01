namespace EmployeeManagement.Api.Models
{
    public class Title
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int DepartmentId { get; set; }  // hangi departmana ait olduğunu tutar
        public Department Department { get; set; } = null!;  // navigation property, DepartmentId'ye bakıp Department bilgisini otomatik getirir
    }
}