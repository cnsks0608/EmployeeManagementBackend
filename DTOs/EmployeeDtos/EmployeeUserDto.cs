namespace EmployeeManagement.Api.DTOs.EmployeeDtos
{
    public class EmployeeUserDto
    {
        public int Id { get; set; } // frontendin id bilgisine ihtiyacı olacağı için yazdık ancak kullanıcıya göstermeyeceğiz 
        public string RegistrationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateOnly HireDate { get; set; }

        public int TitleId { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }
}