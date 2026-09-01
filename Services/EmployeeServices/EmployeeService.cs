using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Enums;

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
            int pageSize = 10,
            string status = "active",
            int? departmentId = null,
            int? titleId = null)


        {
            var query = _context.Employees
                .Include(e => e.Title)   // lazy loadingi önlemek için -> diğer türlü sadece employees tablosuyla alakalı bilgileri getirir, bunları getirmez
                .ThenInclude(t => t.Department)
                .AsQueryable();
              // henüz çalıştırılmamış Employees tablosu üzerinde yapılacak taslak sorgu

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

            if (status == "deleted")
            {
                query = query.Where(e => e.RowStatus == RowStatus.Deleted);
            }
            else
            {
                query = query.Where(e => e.RowStatus == RowStatus.Created || e.RowStatus == RowStatus.Updated);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.Title.DepartmentId == departmentId.Value);  // department bilgileri doğrudan employee entitysinde yok önce employee entitysinde titlea erişip oradan departmenta geçmeliyiz
            }

            if (titleId.HasValue)
            {
                query = query.Where(e => e.TitleId == titleId.Value);
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
                TotalPages = totalPages,

            };
        }



        public async Task<EmployeeAdminDto> GetEmployeeByIdAsync(int id) // burada admin veya user ile ilgili herhangi bir yetki kontrolü yapmadık (user, rowstatusu deleted olan birine erişemez gibi), controllerda yapıcaz
        {
            var employee = await _context.Employees
                .Include(e => e.Title)  // Dönüş değeri olarak Title bilgisi içeren bir DTO döndüren her metoda Include eklendi
                .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(e => e.Id == id);  // veritabanında Employees tablosunda ilgili id'ye sahip kullanıcı bulunur (FindAsync yerine, Include kullanabilmek için FirstOrDefaultAsync kullanıyoruz)

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

            await _context.Entry(employee).Reference(e => e.Title).LoadAsync();  // yeni eklenen employee'nin Title bilgisini de yüklüyoruz (TitleName/DepartmentName doğru dönsün diye)
            await _context.Entry(employee.Title).Reference(t => t.Department).LoadAsync();

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);   // employee modelinden employeeadmindto ya çevrilir ve return edilir
            return employeeDto;
        }

        public async Task<EmployeeAdminDto> UpdateEmployeeByAdminAsync(int id, UpdateEmployeeByAdminDto updateEmployeeByAdminDto)
        {
            var employee = await _context.Employees
                .Include(e => e.Title)
                .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(e => e.Id == id);  // veritabanında Employees tablosunda ilgili id ye sahip kullanıcı bulunur ve employee değişkenine atanır 

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
            employee.TitleId = updateEmployeeByAdminDto.TitleId;

            employee.RowStatus = RowStatus.Updated;  // güncelleme yapıldığı için RowStatus'u Updated yapıyoruz

            await _context.SaveChangesAsync();   // veritabanında bu değişiklikler kornur 

            await _context.Entry(employee).Reference(e => e.Title).LoadAsync();  // TitleId değişmiş olabileceği için, Title bilgisini güncel haliyle tekrar yüklüyoruz
            await _context.Entry(employee.Title).Reference(t => t.Department).LoadAsync();

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);   // employee modelinde olan değişken maplenerek admindto ya çevrilir ve return edilir 
            return employeeDto;
        }

        public async Task DeleteEmployeeByAdminAsync(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            employee.RowStatus = RowStatus.Deleted;  // soft delete

            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);  // bu employee'ye bağlı bir User hesabı var mı diye bakıyoruz
            if (linkedUser != null)
            {
                linkedUser.RowStatus = RowStatus.Deleted;  // varsa, o User hesabını da otomatik olarak Deleted yapıyoruz
            }
            await _context.SaveChangesAsync();
        }


        public async Task<EmployeeAdminDto> ReactivateEmployeeAsync(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Title)
                .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            if (employee.RowStatus != RowStatus.Deleted)
            {
                throw new Exception("Bu çalışan zaten aktif.");
            }

            employee.RowStatus = RowStatus.Updated;

            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);  // bu employee'ye bağlı bir User hesabı var mı diye bakıyoruz
            if (linkedUser != null)
            {
                linkedUser.RowStatus = RowStatus.Updated;  // varsa, o User hesabını da otomatik olarak tekrar aktif ediyoruz
            }

            await _context.SaveChangesAsync();

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);
            return employeeDto;
        }

    }
}