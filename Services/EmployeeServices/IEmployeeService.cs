using EmployeeManagement.Api.DTOs.EmployeeDtos;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Enums;

namespace EmployeeManagement.Api.Services.EmployeeServices
{
    public interface IEmployeeService
    {

        Task<PagedResult<EmployeeAdminDto>> GetAllEmployeesAsync(  // birden fazla dto döndüğü için 
            string? search,
            string? email,     // bu fonksiyon bu alanları parametre olarak alacak, filtreleme ve arama için 
            string? registrationNumber,
            decimal? minSalary,
            decimal? maxSalary,   // ? var çünkü hiçbir filtreleme veya arama yapmayabiliriz yani null olabilir 
            DateOnly? startHireDate,
            DateOnly? endHireDate,  // servis katmanındaki GetAllEmployeesAsync fonksiyonu çalıştıktan sonra geriye employeeadmindto daki alanları return edecek
            int pageNumber = 1,   // varsayılan: 1. sayfa (belirtilmezse otomatik bu değer kullanılır)
            int pageSize = 10,  // varsayılan: sayfa başına 10 kayıt
            string status = "active",
            int? departmentId = null,
            int? titleId = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? hasNoUser = null);


        Task<EmployeeAdminDto> GetEmployeeByIdAsync(int id);
        Task<EmployeeAdminDto> CreateEmployeeByAdminAsync(CreateEmployeeByAdminDto createEmployeeByAdminDto); // parametre olarak kullanıcının gönderdiği veriyi alıyor (createemployeeByAdmindto), geriye employeeadmindto return ediyor bu sefer id si de dönüyor 
        Task<EmployeeAdminDto> UpdateEmployeeByAdminAsync(int id, UpdateEmployeeByAdminDto updateEmployeeByAdminDto);
        Task DeleteEmployeeByAdminAsync(int id);
        Task<EmployeeAdminDto> ReactivateEmployeeAsync(int id);
    }
}