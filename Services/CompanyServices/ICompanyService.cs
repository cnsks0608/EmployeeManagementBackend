using EmployeeManagement.Api.DTOs.CompanyDtos;

namespace EmployeeManagement.Api.Services.CompanyServices
{
    public interface ICompanyService
    {
        Task<List<DepartmentDto>> GetAllDepartmentsAsync();
        Task<List<TitleDto>> GetTitlesByDepartmentsIdAsync(int departmentId);
    }
}