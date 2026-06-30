using Booking_SaaS.Services.Abstraction.DTOs.Tenant;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Tenant;

public class TenantValidator : AbstractValidator<TenantAddDto>
{
    public TenantValidator()
    {
        RuleFor(d => d.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(50)
            .WithMessage("Name cannot exceed 50 characters");
    }
}