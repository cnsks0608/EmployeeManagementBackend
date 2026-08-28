using FluentValidation;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Validators
{
    public class CreateUserByAdminValidator : AbstractValidator<CreateUserByAdminDto>
    {
        private readonly AppDbContext _context;

        public CreateUserByAdminValidator(AppDbContext context)
        {
            _context = context;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
                .MustAsync(BeUniqueUsername).WithMessage("Bu kullanıcı adı zaten kullanılıyor.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .MustAsync(BeUniqueEmail).WithMessage("Bu email adresi zaten kullanılıyor.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre boş bırakılamaz.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Şifre tekrarı boş bırakılamaz.")
                .Equal(x => x.Password).WithMessage("Şifreler birbiriyle eşleşmiyor.");

            RuleFor(x => x.EmployeeId)
                .MustAsync(EmployeeExists).WithMessage("Bu Id'ye sahip bir çalışan bulunamadı.")  // mustasync aşağıda yazdığımız kontrol fonksyionlarını çağırır  
                .MustAsync(EmployeeNotAlreadyLinked).WithMessage("Bu çalışana ait zaten bir kullanıcı hesabı var.");

            RuleFor(x => x.RoleType)
                .IsInEnum().WithMessage("Geçerli bir rol seçilmelidir.");  // enumlar null olamaz ancak hiçbir şey işaretlenmeden devam edilğirse 0 atanır. Bizim 0 diye bir rolümüz olmadığı için hata mesajı göndeririz 
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        }

        private async Task<bool> EmployeeExists(int employeeId, CancellationToken cancellationToken)
        {
            return await _context.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        }

        private async Task<bool> EmployeeNotAlreadyLinked(int employeeId, CancellationToken cancellationToken)
        {
            return !await _context.Users.AnyAsync(u => u.EmployeeId == employeeId, cancellationToken);
        }
    }
}