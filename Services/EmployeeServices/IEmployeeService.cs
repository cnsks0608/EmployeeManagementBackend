using EmployeeManagement.Api.DTOs.EmployeeDtos;

namespace EmployeeManagement.Api.Services.EmployeeServices
{
    public interface IEmployeeService
    {
        Task<List<EmployeeAdminDto>> GetAllEmployeesAsync(
            string? search,
            string? email,     // bu fonksiyon bu alanları parametre olarak alacak, filtreleme ve arama için 
            string? registrationNumber,
            decimal? minSalary,
            decimal? maxSalary,   // ? var çünkü hiçbir filtreleme veya arama yapmayabiliriz yani null olabilir 
            DateOnly? startHireDate,
            DateOnly? endHireDate);  // servis katmanındaki GetAllEmployeesAsync fonksiyonu çalıştıktan sonra geriye employeeadmindto daki alanları return edecek
        Task<EmployeeAdminDto> GetEmployeeByIdAsync(int id);
        Task<EmployeeAdminDto> CreateEmployeeByAdminAsync(CreateEmployeeByAdminDto createEmployeeByAdminDto); // parametre olarak kullanıcının gönderdiği veriyi alıyor (createemployeeByAdmindto), geriye employeeadmindto return ediyor bu sefer id si de dönüyor 
        Task<EmployeeAdminDto> UpdateEmployeeByAdminAsync(int id, UpdateEmployeeByAdminDto updateEmployeeByAdminDto);
        Task DeleteEmployeeByAdminAsync(int id);
    }
}