using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Authentication;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(d => d.OldPassword)
            .NotEmpty()
            .WithMessage("Old password must have a value");

        RuleFor(d => d.NewPassword)
            .NotEmpty()
            .WithMessage("New password must have a value")
            .NotEqual(d => d.OldPassword)
            .WithMessage("Old password and new password must be different");
    }
}