using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Validators.Resource;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators;

public class ResourceValidatorTests
{
    private readonly ResourceValidator _resourceValidator = new();

    #region TestData

    public static TheoryData<string?> NullOrEmptyValues =>
        new()
        {
            { "" },
            { null }
        };

    #endregion

    [Theory]
    [MemberData(nameof(NullOrEmptyValues))]
    public void ShouldFailWhenNameIsNullOrEmpty(string? name)
    {
        var dto = new ResourceAddDto() { Name = name! };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldPassWhenNameIsValid()
    {
        var dto = new ResourceAddDto() { Name = "a" };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldPassWhenNameMatchesMaxLength()
    {
        var dto = new ResourceAddDto() { Name = new string('a', 50) };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldFailWhenNameExceedsMaxLength()
    {
        var dto = new ResourceAddDto() { Name = new string('a', 51) };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Name);
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyValues))]
    public void ShouldFailWhenDescriptionIsNullOrEmpty(string? description)
    {
        var dto = new ResourceAddDto() { Description = description! };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Description);
    }

    [Fact]
    public void ShouldPassWhenDescriptionIsValid()
    {
        var dto = new ResourceAddDto() { Description = "a" };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Description);
    }

    [Fact]
    public void ShouldPassWhenDescriptionMatchesMaxLength()
    {
        var dto = new ResourceAddDto() { Description = new string('a', 500) };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Description);
    }

    [Fact]
    public void ShouldFailWhenDescriptionExceedsMaxLength()
    {
        var dto = new ResourceAddDto() { Description = new string('a', 501) };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Description);
    }

    [Fact]
    public void ShouldFailWhenTypeIsNull()
    {
        var dto = new ResourceAddDto() { Type = null! };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Type);
    }

    [Theory]
    [InlineData("Room")]
    [InlineData("room")]
    [InlineData("Car")]
    [InlineData("car")]
    public void ShouldPassWhenTypeIsValid(string type)
    {
        var dto = new ResourceAddDto() { Type = type };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("Room1")]
    [InlineData("room1")]
    public void ShouldFailWhenTypeIsInvalid(string type)
    {
        var dto = new ResourceAddDto() { Type = type };
        var result = _resourceValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}