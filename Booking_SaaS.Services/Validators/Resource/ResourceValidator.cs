using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Resource;

public class ResourceValidator : AbstractValidator<ResourceAddDto>
{
    public ResourceValidator()
    {
        RuleFor(d => d.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(50)
            .WithMessage("Name cannot exceed 50 characters");

        RuleFor(d => d.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(d => d.Type)
            .NotEmpty()
            .WithMessage("Type is required")
            .IsEnumName(typeof(ResourceType), false)
            .WithMessage("Invalid resource type");
    }
}