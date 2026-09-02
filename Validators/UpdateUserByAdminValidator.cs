using FluentValidation;
using Microsoft.EntityFrameworkCore;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Validators
{
    public class UpdateUserByAdminValidator : AbstractValidator<UpdateUserByAdminDto>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;  // URL'deki id parametresini okumak için

        public UpdateUserByAdminValidator(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı alanı boş bırakılamaz.")
                .MustAsync(BeUniqueUsername).WithMessage("Bu kullanıcı adı zaten kullanılıyor.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .MustAsync(BeUniqueEmail).WithMessage("Bu email adresi zaten kullanılıyor.");

            RuleFor(x => x.RoleType)
                .IsInEnum().WithMessage("Geçerli bir rol seçilmelidir.");
        }

        private int GetCurrentUserId()  // URL'deki id parametresini okur (bu, "current" olan Admin'in id'si değil, GÜNCELLENEN kullanıcının id'si)
        {
            var routeValue = _httpContextAccessor.HttpContext?.Request.RouteValues["id"];
            return routeValue != null ? int.Parse(routeValue.ToString()!) : 0;
        }

        private async Task<bool> BeUniqueUsername(string username, CancellationToken cancellationToken)
        {
            var currentId = GetCurrentUserId();
            return !await _context.Users.AnyAsync(u => u.Username == username && u.Id != currentId, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            var currentId = GetCurrentUserId();
            return !await _context.Users.AnyAsync(u => u.Email == email && u.Id != currentId, cancellationToken);
        }
    }
}