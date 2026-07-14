using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Validators.Authentication;
using FluentValidation.TestHelper;

namespace Booking_SaaS.Tests.Validators.Authentication;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _changePasswordValidator = new();

    [Fact]
    public void ShouldPassWhenDifferentPasswordsExist()
    {
        var dto = new ChangePasswordDto() { OldPassword = "aa", NewPassword = "bb" };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(d => d.NewPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFailWhenOldPasswordIsNullOrEmpty(string? password)
    {
        var dto = new ChangePasswordDto() { OldPassword = password! };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.OldPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ShouldFailWhenNewPasswordIsNullOrEmpty(string? password)
    {
        var dto = new ChangePasswordDto() { NewPassword = password! };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.NewPassword);
    }

    [Fact]
    public void ShouldFailWhenNewPasswordIsEqualToOldPassword()
    {
        var password = "aa";
        var dto = new ChangePasswordDto() { OldPassword = password, NewPassword = password };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(d => d.NewPassword);
    }
}