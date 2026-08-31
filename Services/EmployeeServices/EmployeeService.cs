using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Api.Services.EmployeeServices
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public EmployeeService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<PagedResult<EmployeeAdminDto>> GetAllEmployeesAsync(
            string? search,
            string? email,   // bu parametrelerin sırası interfacetekiyle aynı olmalı yoksa hata alırız
            string? registrationNumber,
            decimal? minSalary,
            decimal? maxSalary,
            DateOnly? startHireDate,
            DateOnly? endHireDate,
            int pageNumber = 1,
            int pageSize = 10)

        {
            var query = _context.Employees.AsQueryable();  // henüz çalıştırılmamış Employees tablosu üzerinde yapılacak taslak sorgu

            if (!string.IsNullOrWhiteSpace(search))  // search boş değilse, isim/soyisim/birleşik üzerinde ara
            {
                var lowerSearch = search.ToLower();  // büyük/küçük harf duyarlılığını ortadan kaldırmak için hem aranan kelimeyi hem veritabanındaki değeri küçük harfe çeviriyoruz
                query = query.Where(e =>
                    e.FirstName.ToLower().Contains(lowerSearch) || // substring mantığıyla arama yapar mesela ben cansu yerine yanlışlıkla canu yazarsam gelmez 
                    e.LastName.ToLower().Contains(lowerSearch) ||
                    (e.FirstName + " " + e.LastName).ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(email))  // email boş değilse, email üzerinde ara
            {
                query = query.Where(e => e.Email.Contains(email));
            }

            if (!string.IsNullOrWhiteSpace(registrationNumber))  // sicil no boş değilse, sicil no üzerinde ara
            {
                var lowerRegistrationNumber = registrationNumber.ToLower();  // sicil numarasında da büyük/küçük harf duyarlılığını kaldırıyoruz
                query = query.Where(e => e.RegistrationNumber.ToLower().Contains(lowerRegistrationNumber));
            }

            if (minSalary.HasValue)  // minSalary verilmişse, o değerden büyük/eşit olanları filtrele
            {
                query = query.Where(e => e.Salary >= minSalary.Value);
            }

            if (maxSalary.HasValue)  // maxSalary verilmişse, o değerden küçük/eşit olanları filtrele
            {
                query = query.Where(e => e.Salary <= maxSalary.Value);
            }

            if (startHireDate.HasValue)  // startHireDate verilmişse, o tarihten sonra işe girenleri filtrele
            {
                query = query.Where(e => e.HireDate >= startHireDate.Value);
            }

            if (endHireDate.HasValue)  // endHireDate verilmişse, o tarihten önce işe girenleri filtrele
            {
                query = query.Where(e => e.HireDate <= endHireDate.Value);
            }

            var totalCount = await query.CountAsync();  // filtrelere uyan TOPLAM kayıt sayısı (sayfalama uygulanmadan ÖNCE sayılmalı eğer sonra yapsaydık sadece o sayfadaki count sayısı gelirdi)
            var skip = (pageNumber - 1) * pageSize;  // kaç kaydın atlanacağını hesaplıyoruz
            query = query.Skip(skip).Take(pageSize);  // ilgili sayfanın kayıtlarını kesiyoruz
            var employees = await query.ToListAsync();  // sadece o sayfadaki kayıtları veritabanından çekiyoruz
            var employeeDtos = _mapper.Map<List<EmployeeAdminDto>>(employees);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<EmployeeAdminDto>
            {
                Items = employeeDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }
        
        

        public async Task<EmployeeAdminDto> GetEmployeeByIdAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);  // veritabanında Employees tablosunda ilgili id'ye sahip kullanıcı bulunur

            if (employee == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);
            return employeeDto;
        }


        public async Task<EmployeeAdminDto> CreateEmployeeByAdminAsync(CreateEmployeeByAdminDto createEmployeeByAdminDto)
        {
            var employee = _mapper.Map<Employee>(createEmployeeByAdminDto); // createemployeedto formatında girdiğimiz verileri employee modeline çevirir

            _context.Employees.Add(employee);  // employees tablosuna bu employee eklenir 
            await _context.SaveChangesAsync();   // değişiklikler veritabanına kaydedilir

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);   // employee modelinden employeeadmindto ya çevrilir ve return edilir
            return employeeDto;
        }

        public async Task<EmployeeAdminDto> UpdateEmployeeByAdminAsync(int id, UpdateEmployeeByAdminDto updateEmployeeByAdminDto)
        {
            var employee = await _context.Employees.FindAsync(id);  // veritabanında Employees tablosunda ilgili id ye sahip kullanıcı bulunur ve employee değişkenine atanır 

            if (employee == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            employee.RegistrationNumber = updateEmployeeByAdminDto.RegistrationNumber;
            employee.FirstName = updateEmployeeByAdminDto.FirstName;
            employee.LastName = updateEmployeeByAdminDto.LastName;
            employee.Email = updateEmployeeByAdminDto.Email;
            employee.Salary = updateEmployeeByAdminDto.Salary;
            employee.HireDate = updateEmployeeByAdminDto.HireDate;     // dto dan gelen bilgiler employee değişkeninin uygun alanlarına atanır 

            await _context.SaveChangesAsync();   // veritabanında bu değişiklikler kornur 

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);   // employee modelinde olan değişken maplenerek admindto ya çevrilir ve return edilir 
            return employeeDto;
        }

        public async Task DeleteEmployeeByAdminAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
        }

    }
}