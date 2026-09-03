using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Validators
{
    public class UpdateMeValidator : AbstractValidator<UpdateMeDto>
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;  // token'dan mevcut kullanıcının id'sini okumak için

        public UpdateMeValidator(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı alanı boş bırakılamaz.")
                .MinimumLength(3).WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
                .MustAsync(BeUniqueUsername).WithMessage("Bu kullanıcı adı zaten kullanılıyor.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş bırakılamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .MustAsync(BeUniqueEmail).WithMessage("Bu email adresi zaten kullanılıyor.");
        }

        private int GetCurrentUserId()  // token'daki NameIdentifier claim'inden mevcut kullanıcının id'sini okur
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!.Value);
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