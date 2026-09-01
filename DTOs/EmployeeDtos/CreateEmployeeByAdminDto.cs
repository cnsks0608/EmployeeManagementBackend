namespace EmployeeManagement.Api.DTOs.EmployeeDtos
{
    public class CreateEmployeeByAdminDto
    {
        public string RegistrationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateOnly HireDate { get; set; }
        public int TitleId { get; set; }
    }
    // yeni employee oluşturulurken id yi veritabanı otomatik atayacak o yüzden bu dto da yok
}