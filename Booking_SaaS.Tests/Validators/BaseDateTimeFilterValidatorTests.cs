using Booking_SaaS.Services.Abstraction.DTOs;
using Booking_SaaS.Services.Validators;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators;

public class DummyDateTimeFilterDto : BaseDateTimeFilterDto { }
public class DummyDateTimeFilterValidator : BaseDateTimeFilterValidator<DummyDateTimeFilterDto> { }

public class BaseDateTimeFilterValidatorTests
{
    private readonly DummyDateTimeFilterValidator _validator = new();

    #region TestData

    public static TheoryData<DateOnly?, DateOnly?> InvalidDateRanges => new()
    {
        { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
        { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }
    };

    public static TheoryData<DateOnly> ValidDateRanges => new()
    {
        { DateOnly.FromDateTime(DateTime.UtcNow) },
        { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }
    };

    public static TheoryData<TimeOnly, TimeOnly> InvalidTimeRanges => new()
    {
        { new TimeOnly(14, 0), new TimeOnly(12, 0) },
        { new TimeOnly(12, 0), new TimeOnly(12, 0) }
    };

    #endregion

    [Fact]
    public void ShouldPassWhenEndDateIsGreaterThanStartDate()
    {
        var dto = new DummyDateTimeFilterDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.EndDate);
    }

    [Theory]
    [MemberData(nameof(InvalidDateRanges))]
    public void ShouldFailWhenEndDateIsLessThanOrEqualToStartDate(DateOnly? start, DateOnly? end)
    {
        var dto = new DummyDateTimeFilterDto { StartDate = start, EndDate = end };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.EndDate);
    }

    [Fact]
    public void ShouldPassWhenEndTimeIsGreaterThanStartTime()
    {
        var dto = new DummyDateTimeFilterDto
        {
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(14, 0)
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.EndTime);
    }

    [Theory]
    [MemberData(nameof(InvalidTimeRanges))]
    public void ShouldFailWhenEndTimeIsLessThanOrEqualToStartTime(TimeOnly start, TimeOnly end)
    {
        var dto = new DummyDateTimeFilterDto { StartTime = start, EndTime = end };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.EndTime);
    }

    [Theory]
    [MemberData(nameof(ValidDateRanges))]
    public void ShouldPassWhenStartDateIsTodayOrInTheFuture(DateOnly date)
    {
        var dto = new DummyDateTimeFilterDto { StartDate = date };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.StartDate);
    }

    [Fact]
    public void ShouldFailWhenStartDateIsInThePast()
    {
        var dto = new DummyDateTimeFilterDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.StartDate);
    }

    [Theory]
    [MemberData(nameof(ValidDateRanges))]
    public void ShouldPassWhenEndDateIsTodayOrInTheFuture(DateOnly date)
    {
        var dto = new DummyDateTimeFilterDto { EndDate = date };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.EndDate);
    }

    [Fact]
    public void ShouldFailWhenEndDateIsInThePast()
    {
        var dto = new DummyDateTimeFilterDto
        {
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.EndDate);
    }
}