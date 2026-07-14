using Booking_SaaS.Services.Abstraction.DTOs.Tenant;
using Booking_SaaS.Services.Validators.Tenant;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators.Tenant;

public class TenantValidatorTests
{
    private readonly TenantValidator _tenantValidator = new();

    [Fact]
    public void ShouldPassWhenNameIsUnderLimit()
    {
        var dto = new TenantAddDto() { Name = "a" };
        var result = _tenantValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldPassWhenNameMatchesLimit()
    {
        var dto = new TenantAddDto() { Name = new string('a', 50) };
        var result = _tenantValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.Name);
    }

    [Fact]
    public void ShouldFailWhenNameLengthExceedsLimit()
    {
        var dto = new TenantAddDto() { Name = new string('a', 60) };
        var result = _tenantValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFailWhenNameIsNullOrEmpty(string? name)
    {
        var dto = new TenantAddDto() { Name = name! };
        var result = _tenantValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.Name);
    }
}