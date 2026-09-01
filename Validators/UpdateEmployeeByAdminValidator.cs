using FluentValidation;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.EmployeeDtos;

namespace EmployeeManagement.Api.Validators
{
    public class UpdateEmployeeByAdminValidator : AbstractValidator<UpdateEmployeeByAdminDto>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;  // Normalde validator sadece dtoyu görebilir ama ben update kısmında url ye id vereceğim için url yi de (id) okuyabilmesi lazım

        // validatorda uniqueness kontrolü sırasında bu id okuma kısmını vs eklemezsek mesela sicil no yu değiştirmek istemediğimizde ama başka bir alanı güncellemek istediğimizde veritabanında bu sicil no ile kayıtlı kişi var diye hata atar 
        // o yüzden uniqueness kontrolü için veritabanında bu id li kullanıcının dışındaki kullanıcıları kontrol edeceğiz 
        public UpdateEmployeeByAdminValidator(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.RegistrationNumber)
                .NotEmpty().WithMessage("Sicil numarası boş bırakılamaz.")
                .MinimumLength(4).WithMessage("Sicil numarası en az 4 karakter olmalıdır.")
                .MustAsync(BeUniqueRegistrationNumber).WithMessage("Bu sicil numarası zaten kullanılıyor.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .MustAsync(BeUniqueEmail).WithMessage("Bu email adresi zaten kullanılıyor.");

            RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Maaş alanı boş bırakılamaz ve 0'dan büyük olmalıdır.");

            RuleFor(x => x.HireDate)
                .NotEmpty().WithMessage("İşe giriş tarihi boş bırakılamaz.")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("İşe giriş tarihi gelecekte olamaz.");

            RuleFor(x => x.TitleId)
                .MustAsync(TitleExists).WithMessage("Bu Id'ye sahip bir ünvan bulunamadı.");
        }

        private int GetCurrentEmployeeId()
        {
            var routeValue = _httpContextAccessor.HttpContext?.Request.RouteValues["id"];
            return routeValue != null ? int.Parse(routeValue.ToString()!) : 0;   // url den id kısmını çekiyoruz
        }

        private async Task<bool> BeUniqueRegistrationNumber(string registrationNumber, CancellationToken cancellationToken)
        {
            var currentId = GetCurrentEmployeeId();  // id sini aldığımız kullanıcının id sini currentid değişkenine atarız 
            return !await _context.Employees.AnyAsync(e => e.RegistrationNumber == registrationNumber && e.Id != currentId, cancellationToken);
            // uniqueness kontolü yaparken currentid ye sahip kullanıcı dışındaki kullanıcıları kontrol ederiz 
            // eğer false dönerse sorun yoktur -> mevcut kullanıcı dışında başka bir kullanıcıda o değer yoktur, true dönerse sorun vardır hata mesajı gönderir
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            var currentId = GetCurrentEmployeeId();
            return !await _context.Employees.AnyAsync(e => e.Email == email && e.Id != currentId, cancellationToken);
        }

        private async Task<bool> TitleExists(int titleId, CancellationToken cancellationToken)
        {
            return await _context.Titles.AnyAsync(t => t.Id == titleId, cancellationToken);
        }
    }
}