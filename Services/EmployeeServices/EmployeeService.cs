using AutoMapper;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.EmployeeDtos;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Exceptions;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Enums;
using EmployeeManagement.Api.Services.LogServices;

namespace EmployeeManagement.Api.Services.EmployeeServices
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;  // o anki contextin içeriğine ulaşabilmek için (method, path, body, statuscode, header, token...)
        private readonly string? _currentUsername;

        public EmployeeService(AppDbContext context, IMapper mapper, IActivityLogService activityLogService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
            _currentUsername = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        }

        private string? GetCurrentUsername()  // giriş yapmış kullanıcının username'ini token'dan okur, loglarda "kim yaptı" bilgisi için kullanılacak (user da da id için yapmıştık -> yardımcı method olduğu içi interface te tanımlaya gerek yoktu)
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
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
            int pageSize = 40,
            string status = "active",
            int? departmentId = null,
            int? titleId = null,
            string? sortBy = null,
            string? sortDirection = null,
            bool? hasNoUser = null)


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
                var lowerEmail = email.ToLower();
                query = query.Where(e => e.Email.ToLower().Contains(lowerEmail));
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

            if (status == "active")
            {
                query = query.Where(e => e.RowStatus != RowStatus.Deleted);
            }

            else if (status == "deleted")
            {
                query = query.Where(e => e.RowStatus == RowStatus.Deleted);
            }

            if (hasNoUser == true)
            {
                var employeeIdsWithUser = await _context.Users.Select(u => u.EmployeeId).ToListAsync();
                query = query.Where(e => !employeeIdsWithUser.Contains(e.Id));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.Title.DepartmentId == departmentId.Value);  // department bilgileri doğrudan employee entitysinde yok önce employee entitysinde titlea erişip oradan departmenta geçmeliyiz
            }

            if (titleId.HasValue)
            {
                query = query.Where(e => e.TitleId == titleId.Value);
            }

            if (sortBy == "salary")  // Bu kısmı gereksiz kayıtlar üzerinde de karşılşatırma ve sıralama işlemi yapmamak için bütün filtrelerden sonra yaptık 
            {
                query = sortDirection == "desc"
                    ? query.OrderByDescending(e => e.Salary)
                    : query.OrderBy(e => e.Salary);  // OrderBy -> ascending demek
            }
            else if (sortBy == "hireDate")
            {
                query = sortDirection == "desc"
                    ? query.OrderByDescending(e => e.HireDate)
                    : query.OrderBy(e => e.HireDate);
            }
            else if (sortBy == "name")
            {
                query = sortDirection == "desc"
                    ? query.OrderByDescending(e => e.FirstName).ThenByDescending(e => e.LastName)
                    : query.OrderBy(e => e.FirstName).ThenBy(e => e.LastName);
            }

            var totalCount = await query.CountAsync();  // filtrelere uyan TOPLAM kayıt sayısı (sayfalama uygulanmadan ÖNCE sayılmalı eğer sonra yapsaydık sadece o sayfadaki count sayısı gelirdi)
            var skip = (pageNumber - 1) * pageSize;  // kaç kaydın atlanacağını hesaplıyoruz
            query = query.Skip(skip).Take(pageSize);  // ilgili sayfanın kayıtlarını kesiyoruz
            var employees = await query.ToListAsync();  // sadece o sayfadaki kayıtları veritabanından çekiyoruz
            var employeeDtos = _mapper.Map<List<EmployeeAdminDto>>(employees);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var appliedFilters = new List<string>();

            if (!string.IsNullOrWhiteSpace(search))
                appliedFilters.Add($"search: {search}");

            if (!string.IsNullOrWhiteSpace(email))
                appliedFilters.Add($"email: {email}");

            if (!string.IsNullOrWhiteSpace(registrationNumber))
                appliedFilters.Add($"registrationNumber: {registrationNumber}");

            if (minSalary.HasValue)
                appliedFilters.Add($"minSalary: {minSalary}");

            if (maxSalary.HasValue)
                appliedFilters.Add($"maxSalary: {maxSalary}");

            if (startHireDate.HasValue)
                appliedFilters.Add($"startHireDate: {startHireDate}");

            if (endHireDate.HasValue)
                appliedFilters.Add($"endHireDate: {endHireDate}");

            if (departmentId.HasValue)
                appliedFilters.Add($"departmentId: {departmentId}");

            if (titleId.HasValue)
                appliedFilters.Add($"titleId: {titleId}");

            var filterDescription = appliedFilters.Count > 0
                ? $" (Filtreler: {string.Join(", ", appliedFilters)})"
                : "";

            await _activityLogService.LogActivityAsync(
            username: _currentUsername,
            targetName: null,
            action: "Read",
            description: $"{_currentUsername} adlı kullanıcı, çalışan listesini görüntüledi. ({totalCount} kayıt bulundu){filterDescription}",
            isSuccess: true
            );


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
                await _activityLogService.LogActivityAsync(
                   username: _currentUsername,
                   targetName: null,
                   action: "Read",
                   description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan çalışan görüntülenmeye çalışıldı ama bulunamadı.",
                   isSuccess: false,
                   failureReason: "Bu Id'ye sahip bir çalışan bulunamadı.");  // loglama throwdan önce olmalı çünkü throwda da return de olduğu gibi metod hemen sonlanır

                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);

            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: $"{employee.FirstName} {employee.LastName}",
              action: "Read",
              description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanın bilgilerini görüntüledi.",
              isSuccess: true);

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

            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: $"{employee.FirstName} {employee.LastName}",
              action: "Create",
              description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı yeni bir çalışan oluşturdu.",
              isSuccess: true);

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
                await _activityLogService.LogActivityAsync(
                username: _currentUsername,
                targetName: null,
                action: "Update",
                description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan çalışan güncellenmeye çalışıldı ama bulunamadı.",
                isSuccess: false,
                failureReason: "Bu Id'ye sahip bir çalışan bulunamadı."
       );
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            // DEĞİŞİKLİKLERİ, employee GÜNCELLENMEDEN ÖNCE hesaplıyoruz
            var changes = new List<string>();

            if (employee.RegistrationNumber != updateEmployeeByAdminDto.RegistrationNumber)
                changes.Add($"Sicil No: {employee.RegistrationNumber} → {updateEmployeeByAdminDto.RegistrationNumber}");

            if (employee.FirstName != updateEmployeeByAdminDto.FirstName || employee.LastName != updateEmployeeByAdminDto.LastName)
                changes.Add($"İsim: {employee.FirstName} {employee.LastName} → {updateEmployeeByAdminDto.FirstName} {updateEmployeeByAdminDto.LastName}");

            if (employee.Email != updateEmployeeByAdminDto.Email)
                changes.Add($"Email: {employee.Email} → {updateEmployeeByAdminDto.Email}");

            if (employee.Salary != updateEmployeeByAdminDto.Salary)
                changes.Add($"Maaş: {employee.Salary} → {updateEmployeeByAdminDto.Salary}");

            if (employee.HireDate != updateEmployeeByAdminDto.HireDate)
                changes.Add($"İşe Giriş Tarihi: {employee.HireDate} → {updateEmployeeByAdminDto.HireDate}");

            if (employee.TitleId != updateEmployeeByAdminDto.TitleId)
                changes.Add($"Ünvan Id: {employee.TitleId} → {updateEmployeeByAdminDto.TitleId}");


            employee.RegistrationNumber = updateEmployeeByAdminDto.RegistrationNumber;
            employee.FirstName = updateEmployeeByAdminDto.FirstName;
            employee.LastName = updateEmployeeByAdminDto.LastName;
            employee.Email = updateEmployeeByAdminDto.Email;
            employee.Salary = updateEmployeeByAdminDto.Salary;
            employee.HireDate = updateEmployeeByAdminDto.HireDate;     // dto dan gelen bilgiler employee değişkeninin uygun alanlarına atanır 
            employee.TitleId = updateEmployeeByAdminDto.TitleId;

            // Bu Employee'ye bağlı bir User varsa, onun email'ini de, güncelleyelim
            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
            if (linkedUser != null)
            {
                linkedUser.Email = updateEmployeeByAdminDto.Email;
            }
            
            employee.RowStatus = RowStatus.Updated;  // güncelleme yapıldığı için RowStatus'u Updated yapıyoruz

            await _context.SaveChangesAsync();   // veritabanında bu değişiklikler kornur 

            await _context.Entry(employee).Reference(e => e.Title).LoadAsync();  // TitleId değişmiş olabileceği için, Title bilgisini güncel haliyle tekrar yüklüyoruz
            await _context.Entry(employee.Title).Reference(t => t.Department).LoadAsync();

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);   // employee modelinde olan değişken maplenerek admindto ya çevrilir ve return edilir 

            var changeDescription = changes.Count > 0 ? string.Join(", ", changes) : "herhangi bir değişiklik yapılmadı";
            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: $"{employee.FirstName} {employee.LastName}",
              action: "Update",
              description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanın bilgilerini güncelledi: {changeDescription}",
              isSuccess: true);

            return employeeDto;
        }

        public async Task DeleteEmployeeByAdminAsync(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: null,
                  action: "Delete",
                  description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan çalışanı silmeye çalıştı ama bulunamadı.",
                  isSuccess: false,
                  failureReason: "Bu Id'ye sahip bir çalışan bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            employee.RowStatus = RowStatus.Deleted;  // soft delete

            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);  // bu employee'ye bağlı bir User hesabı var mı diye bakıyoruz
            if (linkedUser != null)
            {
                linkedUser.RowStatus = RowStatus.Deleted;  // varsa, o User hesabını da otomatik olarak Deleted yapıyoruz
            }
            await _context.SaveChangesAsync();

            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: $"{employee.FirstName} {employee.LastName}",
              action: "Delete",
              description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanı sildi.",
              isSuccess: true);

            if (linkedUser != null)  // bağlı bir User hesabı da otomatik silindiyse, bunu da ayrı bir log kaydı olarak tutuyoruz
            {
                await _activityLogService.LogActivityAsync(
                 username: _currentUsername,
                 targetName: linkedUser.Username,
                 action: "Delete",
                 description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanı silince, o çalışana bağlı {linkedUser.Username} kullanıcı hesabı da otomatik olarak silindi.",
                 isSuccess: true);
            }
        }


        public async Task<EmployeeAdminDto> ReactivateEmployeeAsync(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Title)
                .ThenInclude(t => t.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                await _activityLogService.LogActivityAsync(
                 username: _currentUsername,
                 targetName: null,
                 action: "Reactivate",
                 description: $"{_currentUsername} adlı kullanıcı, Id'si {id} olan çalışanı tekrar aktif etmeye çalıştı ama bulunamadı.",
                 isSuccess: false,
                 failureReason: "Bu Id'ye sahip bir çalışan bulunamadı.");
                throw new NotFoundException("Bu Id'ye sahip bir çalışan bulunamadı.");
            }

            if (employee.RowStatus != RowStatus.Deleted)
            {
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: $"{employee.FirstName} {employee.LastName}",
                  action: "Reactivate",
                  description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanı tekrar aktif etmeye çalıştı ama zaten aktifti.",
                  isSuccess: false,
                  failureReason: "Bu çalışan zaten aktif.");
                throw new Exception("Bu çalışan zaten aktif.");
            }

            employee.RowStatus = RowStatus.Updated;

            var linkedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);  // bu employee'ye bağlı bir User hesabı var mı diye bakıyoruz
            if (linkedUser != null)
            {
                linkedUser.RowStatus = RowStatus.Updated;  // varsa, o User hesabını da otomatik olarak tekrar aktif ediyoruz
                await _activityLogService.LogActivityAsync(
                  username: _currentUsername,
                  targetName: linkedUser.Username,
                  action: "Reactivate",
                  description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanı tekrar aktif edince, o çalışana bağlı {linkedUser.Username} kullanıcı hesabı da otomatik olarak tekrar aktif edildi.",
                  isSuccess: true);
            }

            await _context.SaveChangesAsync();
            await _activityLogService.LogActivityAsync(
              username: _currentUsername,
              targetName: $"{employee.FirstName} {employee.LastName}",
              action: "Reactivate",
              description: $"{_currentUsername} adlı kullanıcı, {employee.FirstName} {employee.LastName} adlı çalışanı tekrar aktif etti.",
              isSuccess: true);

            var employeeDto = _mapper.Map<EmployeeAdminDto>(employee);
            return employeeDto;
        }

    }
}