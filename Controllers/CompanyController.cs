using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmployeeManagement.Api.Services.CompanyServices;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet("GetAllDepartments")]
        [Authorize]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _companyService.GetAllDepartmentsAsync();
            return Ok(departments);
        }

        [HttpGet("GetTitlesByDepartmentsId/{departmentId}")]
        [Authorize]
        public async Task<IActionResult> GetTitlesByDepartmentsId(int departmentId)
        {
            var titles = await _companyService.GetTitlesByDepartmentsIdAsync(departmentId);
            return Ok(titles);
        }
    }
}