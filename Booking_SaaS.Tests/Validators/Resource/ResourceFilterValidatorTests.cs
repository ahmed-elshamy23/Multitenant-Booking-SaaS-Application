using Booking_SaaS.Services.Abstraction.DTOs.Resource;
using Booking_SaaS.Services.Validators.Resource;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators.Resource;

public class ResourceFilterValidatorTests
{
    private readonly ResourceFilterValidator _resourceFilterValidator = new();

    #region TestData

    public static TheoryData<string?> ValidNameValues =>
        new()
        {
            { null },
            { "" },
            { "  " }
        };

    #endregion

    [Theory]
    [MemberData(nameof(ValidNameValues))]
    public void ShouldPassWhenNameIsNullOrEmpty(string? name)
    {
        var dto = new ResourceFilterDto() { Name = name };
        var result = _resourceFilterValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldPassWhenNameMatchesMaxLength()
    {
        var dto = new ResourceFilterDto() { Name = new string('a', 50) };
        var result = _resourceFilterValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldFailWhenNameExceedsMaxLength()
    {
        var dto = new ResourceFilterDto() { Name = new string('a', 51) };
        var result = _resourceFilterValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldPassWhenTypeIsNull()
    {
        var dto = new ResourceFilterDto() { Type = null };
        var result = _resourceFilterValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Type);
    }

    [Theory]
    [InlineData("Room")]
    [InlineData("room")]
    [InlineData("Car")]
    [InlineData("car")]
    public void ShouldPassWhenTypeIsValid(string type)
    {
        var dto = new ResourceFilterDto() { Type = type };
        var result = _resourceFilterValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("Room1")]
    [InlineData("room1")]
    public void ShouldFailWhenTypeIsInvalid(string type)
    {
        var dto = new ResourceFilterDto() { Type = type };
        var result = _resourceFilterValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }
}