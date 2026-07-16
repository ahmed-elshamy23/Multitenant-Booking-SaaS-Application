using Booking_SaaS.Services.Abstraction.DTOs.Bookings;
using Booking_SaaS.Services.Validators.Booking;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators;

public class BookingValidatorTests
{
    private readonly BookingValidator _bookingValidator = new();

    #region TestData

    public static TheoryData<DateOnly> TodayOrUpcomingDates =>
        new()
        {
            { DateOnly.FromDateTime(DateTime.UtcNow) },
            { DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)) },
            { DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) }
        };

    #endregion

    [Theory]
    [MemberData(nameof(TodayOrUpcomingDates))]
    public void ShouldPassWhenDateIsTodayOrInTheFutureAndWithinTwoMonths(DateOnly date)
    {
        var dto = new BookingAddDto() { Date = date };
        var result = _bookingValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Date);
    }

    [Fact]
    public void ShouldFailWhenDateIsInThePast()
    {
        var dto = new BookingAddDto
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2))
        };
        var result = _bookingValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Date);
    }

    [Fact]
    public void ShouldFailWhenDateIsMoreThanTwoMonthsInAdvance()
    {
        var dto = new BookingAddDto
        {
            Date = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2).AddDays(1)
        };
        var result = _bookingValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Date);
    }

    [Fact]
    public void ShouldPassWhenQuantityIsGreaterThanZero()
    {
        var dto = new BookingAddDto() { Quantity = 1 };
        var result = _bookingValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ShouldFailWhenQuantityIsZeroOrLess(int quantity)
    {
        var dto = new BookingAddDto() { Quantity = quantity };
        var result = _bookingValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Quantity);
    }
}