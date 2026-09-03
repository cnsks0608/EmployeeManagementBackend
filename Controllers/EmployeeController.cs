using Microsoft.AspNetCore.Mvc;
using EmployeeManagement.Api.Services.EmployeeServices;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Services.LogServices;

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
        private readonly IActivityLogService _activityLogService;

        public EmployeeController(
            IEmployeeService employeeService,
            IValidator<CreateEmployeeByAdminDto> createEmployeeValidator,
            IValidator<UpdateEmployeeByAdminDto> updateEmployeeValidator,
            IMapper mapper, IActivityLogService activityLogService)
        {
            _employeeService = employeeService;
            _createEmployeeValidator = createEmployeeValidator;
            _updateEmployeeValidator = updateEmployeeValidator;
            _mapper = mapper;
            _activityLogService = activityLogService;
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
            [FromQuery] int pageSize = 10,
            [FromQuery] string status = "active",
            [FromQuery] int? departmentId = null,
            [FromQuery] int? titleId = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = null)
        {
            var effectiveStatus = User.IsInRole("Admin") ? status : "active"; // normalde herkes status=deleted den silinmiş kullanıcılara ulaşabilir, bunu kontrollü hale getirmek için effectiveStatus adında bir değişken tanımlıyoruz, eğer status ile filtreleme yapmak istiyorsak, kullanıcı adminse status e ne yazdıysa (active, passive gibi) o döner ancak user ne yazarsa yazsın biz zorla status u active yaparız  


            var pagedEmployees = await _employeeService.GetAllEmployeesAsync(
                search, email, registrationNumber, minSalary, maxSalary, startHireDate, endHireDate, pageNumber, pageSize, effectiveStatus, departmentId, titleId, sortBy, sortDirection); // verileri servisten employeeadmindto şeklinde (zengin) alırız

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

                if (employee.RowStatus == RowStatus.Deleted) // kullanıcı admin değilse ve erişmek istediği employeenin row statusu deleted ise 
                {
                    return NotFound("Bu Id'ye sahip bir çalışan bulunamadı.");
                }


                var myEmployeeIdClaim = User.FindFirst("EmployeeId")?.Value;  // kullanıcının kendi bağlı olduğu employeeid sini tokendan okuyoruz
                var myEmployeeId = int.Parse(myEmployeeIdClaim!);

                if (myEmployeeId == id)  // sorgulanan employee, kullanıcının kendi employeesi mi
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
                // controllerda yalnızca validatordan kaynaklı hataları logluyoruz, diğer her şeyi servis katmanında yaptık zaten
                await _activityLogService.LogActivityAsync(
                  username: User.Identity?.Name,
                  targetName: null,
                  action: "Create",
                  description: $"{User.Identity?.Name} adlı kullanıcı, yeni bir çalışan oluşturmaya çalıştı ama girdiği bilgiler geçersizdi.",
                  isSuccess: false,
                  failureReason: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

                return BadRequest(validationResult.Errors);
            }

            try
            {
                var employee = await _employeeService.CreateEmployeeByAdminAsync(createEmployeeByAdminDto);
                return Ok(new { message = "Çalışan başarılı bir şekilde oluşturuldu.", employee });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPut("UpdateEmployeeByAdmin/{id}")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> UpdateEmployeeByAdmin(int id, UpdateEmployeeByAdminDto updateEmployeeByAdminDto)
        {
            var validationResult = await _updateEmployeeValidator.ValidateAsync(updateEmployeeByAdminDto);

            if (!validationResult.IsValid)
            {
                await _activityLogService.LogActivityAsync(
                    username: User.Identity?.Name,
                    targetName: $"{updateEmployeeByAdminDto.FirstName} {updateEmployeeByAdminDto.LastName}",  // aslında bu çok doğru değil, dto ya girilen isim oluyor belki de kullanıcının gerçek ismi o değil ancak diğer türlü controller ın context ile veritabanına erişmesi gerekir ama bunu istemiyoruz
                    action: "Update",
                    description: $"{User.Identity?.Name} adlı kullanıcı, {id} id'li çalışanı güncellemeye çalıştı ama girdiği bilgiler geçersizdi.",
                    isSuccess: false,
                    failureReason: string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

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

        [HttpPut("ReactivateEmployee/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateEmployee(int id)
        {
            try
            {
                var employee = await _employeeService.ReactivateEmployeeAsync(id);
                return Ok(new { message = "Çalışan başarılı bir şekilde tekrar aktif edildi.", employee });
            }
            catch (NotFoundException ex) // o id ye sahip çalışan bulunamadıysa
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);  // kayıt zaten aktifse 
            }
        }
    }
}