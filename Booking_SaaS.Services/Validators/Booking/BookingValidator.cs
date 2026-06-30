using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Booking;

public class BookingValidator : AbstractValidator<BookingAddDto>
{
    public BookingValidator()
    {
        RuleFor(d => d.Date)
            .GreaterThanOrEqualTo(d => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(d => d.Date.HasValue)
            .WithMessage("Date can't be in the past.");

        RuleFor(d => d.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2))
            .When(d => d.Date.HasValue)
            .WithMessage("Booking cannot be created more than 2 months in advance.");

        RuleFor(d => d.Quantity)
            .GreaterThan(0)
            .WithMessage("Number can't be less than 1.");
    }
}