using EmployeeManagement.Api.DTOs.CompanyDtos;

namespace EmployeeManagement.Api.Services.CompanyServices
{
    public interface ICompanyService
    {
        Task<List<DepartmentDto>> GetAllDepartmentsAsync();
        Task<List<TitleDto>> GetTitlesByDepartmentsIdAsync(int departmentId);

        // Frontende bu seçenekleri dropdown olarak göndermek için bu kayıtları veritabnından okumamız gerekir yoksa frontend department ve titlelara nasıl erişecek 
    }
}