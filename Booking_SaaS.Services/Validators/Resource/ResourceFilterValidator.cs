using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Resource;

public class ResourceFilterValidator : AbstractValidator<ResourceFilterDto>
{
    public ResourceFilterValidator()
    {
        RuleFor(r => r.Name)
            .MaximumLength(50)
            .WithMessage("Name cannot exceed 50 characters");

        RuleFor(r => r.Type)
            .IsEnumName(typeof(ResourceType), false)
            .WithMessage("Invalid resource type");
    }
}