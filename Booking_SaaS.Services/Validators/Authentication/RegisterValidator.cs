using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Authentication;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(d => d.FirstName)
            .NotEmpty()
            .WithMessage("First Name is required")
            .Matches("^[a-zA-Z][a-zA-Z_]*$")
            .WithMessage("First Name must contain only alphabetic characters")
            .MaximumLength(50);

        RuleFor(d => d.LastName)
            .NotEmpty()
            .WithMessage("Last Name is required")
            .Matches("^[a-zA-Z][a-zA-Z_]*$")
            .WithMessage("Last Name must contain only alphabetic characters")
            .MaximumLength(50);

        RuleFor(d => d.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone Number is required")
            .Matches(@"^01\d{9}$")
            .WithMessage("Phone Number must contain 11 digits starting with 01");
    }
}