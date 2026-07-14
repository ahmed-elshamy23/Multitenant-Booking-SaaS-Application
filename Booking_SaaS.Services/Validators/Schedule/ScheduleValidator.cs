using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using FluentValidation;

namespace Booking_SaaS.Services.Validators.Schedule;

public class ScheduleValidator : AbstractValidator<ScheduleAddDto>
{
    public ScheduleValidator()
    {
        RuleFor(d => d.Capacity)
            .GreaterThan(0)
            .WithMessage("Capacity must be greater than 0");

        RuleFor(d => d)
            .Must(d => d.DayOfWeek.HasValue ^ d.Date.HasValue)
            .WithMessage("Either DayOfWeek or Date must be provided, but not both");

        RuleFor(d => d.EndTime)
            .GreaterThan(d => d.StartTime)
            .WithMessage("End time must be greater than start time");

        RuleFor(d => d.Date)
            .GreaterThan(d => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(d => d.Date.HasValue)
            .WithMessage("Date can't be in the past");

        RuleFor(d => d.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2))
            .When(d => d.Date.HasValue)
            .WithMessage("Schedules cannot be created more than 2 months in advance");
    }
}