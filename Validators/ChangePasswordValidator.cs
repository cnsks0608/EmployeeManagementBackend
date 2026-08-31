using FluentValidation;
using EmployeeManagement.Api.DTOs.UserDtos;

namespace EmployeeManagement.Api.Validators
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Mevcut şifre boş bırakılamaz.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Yeni şifre boş bırakılamaz.")
                .MinimumLength(6).WithMessage("Yeni şifre en az 6 karakter olmalıdır.")
                .NotEqual(x => x.CurrentPassword).WithMessage("Yeni şifre, mevcut şifreyle aynı olamaz.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Şifre tekrarı boş bırakılamaz.")
                .Equal(x => x.NewPassword).WithMessage("Şifreler birbiriyle eşleşmiyor.");
        }
    }
}