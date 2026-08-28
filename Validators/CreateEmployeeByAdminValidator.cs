using FluentValidation;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.EmployeeDtos;

namespace EmployeeManagement.Api.Validators
{
    public class CreateEmployeeByAdminValidator : AbstractValidator<CreateEmployeeByAdminDto>
    {
        private readonly AppDbContext _context;

        public CreateEmployeeByAdminValidator(AppDbContext context)
        {
            _context = context;  // veritabnına erişemk için -> sicil numarası için

            RuleFor(x => x.RegistrationNumber)
                .NotEmpty().WithMessage("Sicil numarası boş bırakılamaz.")
                .MinimumLength(4).WithMessage("Sicil numarası en az 4 karakter olmalıdır.")
                .MustAsync(BeUniqueRegistrationNumber).WithMessage("Bu sicil numarası zaten kullanılıyor.");  // veritabanından bakar eğer bu sicil numarası zaten varsa hata mesajı yollar (stm de uniqueless kontrolünü servicete yapmıştım burada farklı olsun diye validatorda yaptım)

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad alanı boş bırakılamaz.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad alanı boş bırakılamaz.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .MustAsync(BeUniqueEmail).WithMessage("Bu email adresi zaten kullanılıyor.");

            RuleFor(x => x.Salary)
                .GreaterThan(0).WithMessage("Maaş 0'dan büyük olmalıdır.");

            RuleFor(x => x.HireDate)
                .NotEmpty().WithMessage("İşe giriş tarihi boş bırakılamaz.")
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).WithMessage("İşe giriş tarihi gelecekte olamaz.");
        }

        private async Task<bool> BeUniqueRegistrationNumber(string registrationNumber, CancellationToken cancellationToken)
        {
            return !await _context.Employees.AnyAsync(e => e.RegistrationNumber == registrationNumber, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Employees.AnyAsync(e => e.Email == email, cancellationToken);
        }
    }
}