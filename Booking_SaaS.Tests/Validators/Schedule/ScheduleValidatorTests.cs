using Booking_SaaS.Services.Abstraction.DTOs.Schedule;
using Booking_SaaS.Services.Validators.Schedule;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators.Schedule;

public class ScheduleValidatorTests
{
    private readonly ScheduleValidator _validator = new();

    #region TestData

    public static TheoryData<TimeOnly, TimeOnly> InvalidStartEndTimePairs =>
    new()
    {
            { new TimeOnly(12, 0), new TimeOnly(12, 0) },
            { new TimeOnly(12, 0), new TimeOnly(10, 0) }
    };
    public static TheoryData<DayOfWeek?, DateOnly?> ValidDayOfWeekAndDateCombinations =>
        new()
        {
            { DayOfWeek.Monday, null },
            { null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) }
        };

    public static TheoryData<DateOnly> UpcomingDates =>
        new()
        {
            { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)) },
            { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) }
        };

    public static TheoryData<DateOnly> TodayOrPastDates =>
        new()
        {
            { DateOnly.FromDateTime(DateTime.UtcNow) },
            { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) }
        };

    #endregion

    [Fact]
    public void ShouldPassWhenCapacityIsGreaterThanZero()
    {
        var dto = new ScheduleAddDto { Capacity = 1 };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Capacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ShouldFailWhenCapacityIsZeroOrLess(int capacity)
    {
        var dto = new ScheduleAddDto { Capacity = capacity };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Capacity);
    }

    [Theory]
    [MemberData(nameof(ValidDayOfWeekAndDateCombinations))]
    public void ShouldPassWhenOnlyDayOfWeekOrDateIsProvided(DayOfWeek? dayOfWeek, DateOnly? date)
    {
        var dto = new ScheduleAddDto
        {
            DayOfWeek = dayOfWeek,
            Date = date
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d);
    }

    [Fact]
    public void ShouldFailWhenBothDayOfWeekAndDateAreProvided()
    {
        var dto = new ScheduleAddDto
        {
            DayOfWeek = DayOfWeek.Monday,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d);
    }

    [Fact]
    public void ShouldFailWhenNeitherDayOfWeekNorDateAreProvided()
    {
        var dto = new ScheduleAddDto
        {
            DayOfWeek = null,
            Date = null
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d);
    }

    [Fact]
    public void ShouldPassWhenEndTimeIsGreaterThanStartTime()
    {
        var dto = new ScheduleAddDto
        {
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0)
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.EndTime);
    }

    [Theory]
    [MemberData(nameof(InvalidStartEndTimePairs))]
    public void ShouldFailWhenEndTimeIsLessThanOrEqualToStartTime(TimeOnly startTime, TimeOnly endTime)
    {
        var dto = new ScheduleAddDto
        {
            StartTime = startTime,
            EndTime = endTime
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.EndTime);
    }

    [Theory]
    [MemberData(nameof(UpcomingDates))]
    public void ShouldPassWhenDateIsInTheFutureAndWithinTwoMonths(DateOnly date)
    {
        var dto = new ScheduleAddDto { Date = date };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Date);
    }

    [Theory]
    [MemberData(nameof(TodayOrPastDates))]
    public void ShouldFailWhenDateIsTodayOrInThePast(DateOnly date)
    {
        var dto = new ScheduleAddDto { Date = date };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Date);
    }

    [Fact]
    public void ShouldFailWhenDateIsMoreThanTwoMonthsInAdvance()
    {
        var dto = new ScheduleAddDto
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2).AddDays(1)
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Date);
    }
}