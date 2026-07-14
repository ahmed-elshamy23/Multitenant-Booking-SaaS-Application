using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Validators.Authentication;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    #region TestData

    public static TheoryData<string?> NullOrEmptyValues =>
        new()
        {
            { "" },
            { null }
        };

    public static TheoryData<string> ValidNameValues =>
        new()
        {
            { "aa" },
            { "a_a" }
        };

    public static TheoryData<string> InvalidNameValues =>
        new()
        {
            { "1a" },
            { "_a" },
            { "a a" },
            { "a-a" },
            { "  a" },
            { "   " }
        };

    public static TheoryData<string> ValidPhoneNumbers =>
        new()
        {
            { "01512345678" },
            { "01234567890" },
            { "01123456789" },
            { "01012345678" }
        };

    public static TheoryData<string> InvalidPhoneNumbers =>
        new()
        {
            { " 01012345678" },
            { "0101234abc8" },
            { "010123456789" },
            { "0101234567" },
            { "02012345678" },
            { "11012345678" }
        };

    #endregion

    [Theory]
    [MemberData(nameof(ValidNameValues))]
    public void ShouldPassWhenFirstNameIsValid(string firstName)
    {
        var dto = new RegisterDto { FirstName = firstName };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.FirstName);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyValues))]
    public void ShouldFailWhenFirstNameIsNullOrEmpty(string? firstName)
    {
        var dto = new RegisterDto { FirstName = firstName! };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.FirstName);
    }

    [Theory]
    [MemberData(nameof(InvalidNameValues))]
    public void ShouldFailWhenFirstNameContainsInvalidCharacters(string firstName)
    {
        var dto = new RegisterDto { FirstName = firstName };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.FirstName);
    }

    [Fact]
    public void ShouldPassWhenFirstNameMatchesMaxLength()
    {
        var dto = new RegisterDto { FirstName = new string('a', 50) };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.FirstName);
    }

    [Fact]
    public void ShouldFailWhenFirstNameExceedsMaxLength()
    {
        var dto = new RegisterDto { FirstName = new string('a', 51) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.FirstName);
    }

    [Theory]
    [MemberData(nameof(ValidNameValues))]
    public void ShouldPassWhenLastNameIsValid(string lastName)
    {
        var dto = new RegisterDto { LastName = lastName };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.LastName);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyValues))]
    public void ShouldFailWhenLastNameIsNullOrEmpty(string? lastName)
    {
        var dto = new RegisterDto { LastName = lastName! };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.LastName);
    }

    [Fact]
    public void ShouldPassWhenLastNameMatchesMaxLength()
    {
        var dto = new RegisterDto { LastName = new string('a', 50) };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.LastName);
    }

    [Fact]
    public void ShouldFailWhenLastNameExceedsMaxLength()
    {
        var dto = new RegisterDto { LastName = new string('a', 51) };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.LastName);
    }

    [Theory]
    [MemberData(nameof(InvalidNameValues))]
    public void ShouldFailWhenLastNameContainsInvalidCharacters(string lastName)
    {
        var dto = new RegisterDto { LastName = lastName };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.LastName);
    }

    [Theory]
    [MemberData(nameof(ValidPhoneNumbers))]
    public void ShouldPassWhenPhoneNumberIsValid(string phoneNumber)
    {
        var dto = new RegisterDto { PhoneNumber = phoneNumber };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.PhoneNumber);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyValues))]
    public void ShouldFailWhenPhoneNumberIsNullOrEmpty(string? phoneNumber)
    {
        var dto = new RegisterDto { PhoneNumber = phoneNumber! };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.PhoneNumber);
    }

    [Theory]
    [MemberData(nameof(InvalidPhoneNumbers))]
    public void ShouldFailWhenPhoneNumberIsInvalidFormat(string phoneNumber)
    {
        var dto = new RegisterDto { PhoneNumber = phoneNumber };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.PhoneNumber);
    }
}