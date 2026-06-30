using Booking_SaaS.Services.Abstraction.DTOs;
using FluentValidation;

namespace Booking_SaaS.Services.Validators;

public class BaseDateTimeFilterValidator<T> : AbstractValidator<T> where T : BaseDateTimeFilterDto
{
    public BaseDateTimeFilterValidator()
    {
        RuleFor(d => d.EndTime)
            .GreaterThan(d => d.StartTime)
            .When(d => d.EndTime.HasValue && d.StartTime.HasValue)
            .WithMessage("End time must be greater than start time");

        RuleFor(d => d.EndDate)
            .GreaterThan(d => d.StartDate)
            .When(d => d.EndDate.HasValue && d.StartDate.HasValue)
            .WithMessage("End date must be greater than start date");

        RuleFor(d => d.StartDate)
            .GreaterThanOrEqualTo(d => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(d => d.StartDate.HasValue)
            .WithMessage("Start date can't be in the past");

        RuleFor(d => d.EndDate)
            .GreaterThanOrEqualTo(d => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(d => d.EndDate.HasValue)
            .WithMessage("Start date can't be in the past");
    }
}