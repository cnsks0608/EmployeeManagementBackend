using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.CompanyDtos;

namespace EmployeeManagement.Api.Services.CompanyServices
{
    public class CompanyService : ICompanyService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public CompanyService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<DepartmentDto>> GetAllDepartmentsAsync()
        {
            var departments = await _context.Departments.ToListAsync();  // tüm departmanları çekiyoruz
            var departmentDtos = _mapper.Map<List<DepartmentDto>>(departments);
            return departmentDtos;
        }

        public async Task<List<TitleDto>> GetTitlesByDepartmentsIdAsync(int departmentId)
        {
            var titles = await _context.Titles
                .Where(t => t.DepartmentId == departmentId)  // sadece verilen departmana ait ünvanları filtreliyoruz
                .ToListAsync();
            var titleDtos = _mapper.Map<List<TitleDto>>(titles);
            return titleDtos;
        }
    }
}