using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Api.Services.EmployeeServices;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IValidator<CreateEmployeeByAdminDto> _createEmployeeValidator;
        private readonly IValidator<UpdateEmployeeByAdminDto> _updateEmployeeValidator;
        private readonly IMapper _mapper;

        public EmployeeController(
            IEmployeeService employeeService,
            IValidator<CreateEmployeeByAdminDto> createEmployeeValidator,
            IValidator<UpdateEmployeeByAdminDto> updateEmployeeValidator,
            IMapper mapper)
        {
            _employeeService = employeeService;
            _createEmployeeValidator = createEmployeeValidator;
            _updateEmployeeValidator = updateEmployeeValidator;
            _mapper = mapper;
        }

        [HttpGet("GetAllEmployees")]
        [Authorize]
        public async Task<IActionResult> GetAllEmployees(
            [FromQuery] string? search,
            [FromQuery] string? email,
            [FromQuery] string? registrationNumber,
            [FromQuery] decimal? minSalary,
            [FromQuery] decimal? maxSalary,
            [FromQuery] DateOnly? startHireDate,
            [FromQuery] DateOnly? endHireDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var pagedEmployees = await _employeeService.GetAllEmployeesAsync(
                search, email, registrationNumber, minSalary, maxSalary, startHireDate, endHireDate, pageNumber, pageSize); // verileri servisten employeeadmindto şeklinde (zengin) alırız

            if (User.IsInRole("Admin"))
            {
                return Ok(pagedEmployees);  // Admin ise olduğu gibi görür (Salary dahil)
            }

            // Admin değilse, Items içindeki listeyi EmployeeUserDto'ya çeviriyoruz, sayfalama bilgilerini koruyoruz
            var userVersion = new PagedResult<EmployeeUserDto>
            {
                Items = _mapper.Map<List<EmployeeUserDto>>(pagedEmployees.Items),
                PageNumber = pagedEmployees.PageNumber,
                PageSize = pagedEmployees.PageSize,
                TotalCount = pagedEmployees.TotalCount,
                TotalPages = pagedEmployees.TotalPages
            };
            return Ok(userVersion);
        }

        [HttpGet("GetEmployeeById/{id}")]
        [Authorize]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(id);

                if (User.IsInRole("Admin"))
                {
                    return Ok(employee);
                }

                var myEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;  // kullanıcının kendi bağlı olduğu employeeid sini tokendan okuyoruz
                var myEmployeeId = int.Parse(myEmployeeIdClaim!);

                if (myEmployeeId == id)  // sorgulanan employee, kullanıcının kendi employeei mi
                {
                    return Ok(employee);  // evetse servisten gelen employeeadmindto ile salary dahil göster
                }

                var userVersion = _mapper.Map<EmployeeUserDto>(employee);  // başkasının bilgisi ise employeeuserdto ile salarysiz göster
                return Ok(userVersion);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("CreateEmployeeByAdmin")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> CreateEmployeeByAdmin(CreateEmployeeByAdminDto createEmployeeByAdminDto)
        {
            var validationResult = await _createEmployeeValidator.ValidateAsync(createEmployeeByAdminDto);  // validatorladan geçip geçemediğini kontrol ediyoruz

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            var employee = await _employeeService.CreateEmployeeByAdminAsync(createEmployeeByAdminDto);
            return Ok(new { message = "Çalışan başarılı bir şekilde oluşturuldu.", employee });
        }

        [HttpPut("UpdateEmployeeByAdmin/{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> UpdateEmployeeByAdmin(int id, UpdateEmployeeByAdminDto updateEmployeeByAdminDto)
        {
            var validationResult = await _updateEmployeeValidator.ValidateAsync(updateEmployeeByAdminDto);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            try
            {
                var employee = await _employeeService.UpdateEmployeeByAdminAsync(id, updateEmployeeByAdminDto);
                return Ok(new { message = "Çalışan başarılı bir şekilde güncellendi.", employee });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }


        [HttpDelete("DeleteEmployeeByAdmin/{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> DeleteEmployeeByAdmin(int id)
        {
            try
            {
                await _employeeService.DeleteEmployeeByAdminAsync(id);
                return Ok(new { message = "Çalışan başarılı bir şekilde silindi." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}