using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Abstraction.Options;
using Booking_SaaS.Services.Specifications;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Booking_SaaS.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IValidator<ChangePasswordDto>> _changePasswordValidator = new();
    private readonly Mock<IValidator<RegisterDto>> _registerValidator = new();
    private readonly Mock<IRefreshTokenRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<UserManager<AppUser>> _userManager = new(
        Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    private readonly AuthenticationService _authenticationService;
    private static readonly IOptions<JwtOptions> _jwtOptions;
    private static readonly string _email = "test@test.com";
    private static readonly string _password = "P@ssw0rd";

    static AuthenticationServiceTests()
    {
        _jwtOptions = Options.Create(new JwtOptions()
        {
            AccessTokenDurationInMinutes = 10,
            Audience = "audience",
            Issuer = "issuer",
            RefreshTokenDurationInDays = 1,
            SecretKey = "kabfg6td76fe78gsaiuLKLHA&IW**%*QG3815698hascs_@#423498"
        });
    }

    public AuthenticationServiceTests()
    {
        _unitOfWork.Setup(x => x.RefreshTokenRepository).Returns(_repo.Object);

        _authenticationService = new AuthenticationService(_userManager.Object,
                                                           _jwtOptions,
                                                           _registerValidator.Object,
                                                           _changePasswordValidator.Object,
                                                           _unitOfWork.Object);
    }

    [Fact]
    public async Task LoginAsync_ShouldFailWhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var result = await _authenticationService.LoginAsync(_email, _password);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidUser);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldFailWhenPasswordIsWrong()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _authenticationService.LoginAsync(_email, _password);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidCredentials);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldPassAndGenerateCorrectTokensWhenCredentialsAreCorrect()
    {
        var id = 1;
        RefreshToken? refreshToken = null;
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser() { Id = 1, Email = _email });

        _userManager.Setup(x => x.CheckPasswordAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _userManager.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(["user"]);

        _repo.Setup(x => x.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => refreshToken = token);

        var result = await _authenticationService.LoginAsync(_email, _password);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        ValidateGeneratedIdAndTokens(id, refreshToken, result);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _registerValidator.Setup(x => x.Validate(It.IsAny<RegisterDto>()))
                          .Returns(new ValidationResult(fakeFailures));

        var dto = new RegisterDto();
        var result = await _authenticationService.RegisterAsync(dto, 1, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.ErrorType.Should().Be(ErrorType.Validation);

        _userManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFailWhenUserAlreadyExist()
    {
        _registerValidator.Setup(x => x.Validate(It.IsAny<RegisterDto>()))
                          .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        var dto = new RegisterDto();
        var result = await _authenticationService.RegisterAsync(dto, 1, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFailWhenUserCreationFails()
    {
        _registerValidator.Setup(x => x.Validate(It.IsAny<RegisterDto>()))
                          .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        _userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var dto = new RegisterDto();
        var result = await _authenticationService.RegisterAsync(dto, 1, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFailWhenAddingToRoleFails()
    {
        _registerValidator.Setup(x => x.Validate(It.IsAny<RegisterDto>()))
                          .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        _userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

        _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var dto = new RegisterDto();
        var result = await _authenticationService.RegisterAsync(dto, 1, true);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldPassWhenUserSuccessfullyCreated()
    {
        _registerValidator.Setup(x => x.Validate(It.IsAny<RegisterDto>()))
                          .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        _userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

        _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

        _userManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()))
                    .ReturnsAsync("Test");

        var dto = new RegisterDto();
        var result = await _authenticationService.RegisterAsync(dto, 1, true);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _userManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldFailWhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var dto = new ConfirmEmailDto() { UserId = "1", Token = "Test" };
        var result = await _authenticationService.ConfirmEmailAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ConfirmEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                            Times.Never);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldFailWhenEmailConfirmationFails()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.ConfirmEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var dto = new ConfirmEmailDto() { UserId = "1", Token = "Test" };
        var result = await _authenticationService.ConfirmEmailAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ConfirmEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                            Times.Once);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ShouldPassWhenEmailConfirmationSucceeds()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.ConfirmEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

        var dto = new ConfirmEmailDto() { UserId = "1", Token = "Test" };
        var result = await _authenticationService.ConfirmEmailAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _userManager.Verify(x => x.ConfirmEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                            Times.Once);
    }

    [Fact]
    public async Task ForgetPasswordAsync_ShouldFailWhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var dto = new ForgotPasswordDto() { Email = _email };
        var result = await _authenticationService.ForgetPasswordAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task ForgetPasswordAsync_ShouldFailWhenFirstNameDoesNotMatch()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser() { FirstName = "1", LastName = "2" });

        var dto = new ForgotPasswordDto() { Email = _email, FirstName = "2", LastName = "2" };
        var result = await _authenticationService.ForgetPasswordAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task ForgetPasswordAsync_ShouldFailWhenLastNameDoesNotMatch()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser() { FirstName = "1", LastName = "2" });

        var dto = new ForgotPasswordDto() { Email = _email, FirstName = "1", LastName = "1" };
        var result = await _authenticationService.ForgetPasswordAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task ForgetPasswordAsync_ShouldPassWhenTokenIsGenerated()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser() { FirstName = "1", LastName = "2" });

        _userManager.Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AppUser>()))
                    .ReturnsAsync("Test");

        var dto = new ForgotPasswordDto() { Email = _email, FirstName = "1", LastName = "2" };
        var result = await _authenticationService.ForgetPasswordAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _userManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<AppUser>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldFailWhenValidationErrorsExist()
    {
        var fakeFailures = new List<ValidationFailure> { new("Property", "Error") };
        _changePasswordValidator.Setup(x => x.Validate(It.IsAny<ChangePasswordDto>()))
                               .Returns(new ValidationResult(fakeFailures));

        var dto = new ChangePasswordDto();
        var result = await _authenticationService.ChangePasswordAsync(dto, "1");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AppUser>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldFailWhenUserNotFound()
    {
        _changePasswordValidator.Setup(x => x.Validate(It.IsAny<ChangePasswordDto>()))
                               .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        var dto = new ChangePasswordDto();
        var result = await _authenticationService.ChangePasswordAsync(dto, "1");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AppUser>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldFailWhenPasswordChangeFails()
    {
        _changePasswordValidator.Setup(x => x.Validate(It.IsAny<ChangePasswordDto>()))
                               .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.ChangePasswordAsync(It.IsAny<AppUser>(),
                                                      It.IsAny<string>(),
                                                      It.IsAny<string>()))
                                .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var dto = new ChangePasswordDto { OldPassword = _password, NewPassword = _password };
        var result = await _authenticationService.ChangePasswordAsync(dto, "1");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AppUser>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldPassWhenPasswordSuccessfullyChanged()
    {
        _changePasswordValidator.Setup(x => x.Validate(It.IsAny<ChangePasswordDto>()))
                               .Returns(new ValidationResult());

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.ChangePasswordAsync(It.IsAny<AppUser>(),
                                                      It.IsAny<string>(),
                                                      It.IsAny<string>()))
                                .ReturnsAsync(IdentityResult.Success);

        var dto = new ChangePasswordDto { OldPassword = _password, NewPassword = _password };
        var result = await _authenticationService.ChangePasswordAsync(dto, "1");

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _userManager.Verify(x => x.ChangePasswordAsync(It.IsAny<AppUser>(),
                                                               It.IsAny<string>(),
                                                               It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldFailWhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        var dto = new ResetPasswordDto();
        var result = await _authenticationService.ResetPasswordAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ResetPasswordAsync(It.IsAny<AppUser>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldFailWhenPasswordResetFails()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.ResetPasswordAsync(It.IsAny<AppUser>(),
                                                      It.IsAny<string>(),
                                                      It.IsAny<string>()))
                                .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var dto = new ResetPasswordDto()
        {
            UserId = "1",
            NewPassword = _password,
            Token = "Test"
        };
        var result = await _authenticationService.ResetPasswordAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();

        _userManager.Verify(x => x.ResetPasswordAsync(It.IsAny<AppUser>(),
                                                       It.IsAny<string>(),
                                                       It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldPassWhenPasswordSuccessfullyReset()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        _userManager.Setup(x => x.ResetPasswordAsync(It.IsAny<AppUser>(),
                                                      It.IsAny<string>(),
                                                      It.IsAny<string>()))
                                .ReturnsAsync(IdentityResult.Success);

        var dto = new ResetPasswordDto()
        {
            UserId = "1",
            NewPassword = _password,
            Token = "Test"
        };
        var result = await _authenticationService.ResetPasswordAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        _userManager.Verify(x => x.ResetPasswordAsync(It.IsAny<AppUser>(),
                                                               It.IsAny<string>(),
                                                               It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task IsOwnerAsync_ByEmail_ShouldFailWhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        var result = await _authenticationService.IsOwnerAsync(_email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidCredentials);
    }

    [Fact]
    public async Task IsOwnerAsync_ByEmail_ShouldFailWhenEmailNotConfirmed()
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                     .ReturnsAsync(new AppUser() { EmailConfirmed = false });

        var result = await _authenticationService.IsOwnerAsync(_email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.EmailNotConfirmed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IsOwnerAsync_ByEmail_ShouldPassAndReturnGivenValueWhenEmailConfirmed(bool isOwner)
    {
        _userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                     .ReturnsAsync(new AppUser() { EmailConfirmed = true, TenantId = isOwner ? null : 1 });

        var result = await _authenticationService.IsOwnerAsync(_email);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(isOwner);
    }

    [Fact]
    public async Task IsOwnerAsync_ById_ShouldFailWhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                     .ReturnsAsync((AppUser?)null);

        var result = await _authenticationService.IsOwnerAsync(1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidUser);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IsOwnerAsync_ById_ShouldPassAndReturnGivenValueWhenUserExists(bool isOwner)
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                     .ReturnsAsync(new AppUser() { TenantId = isOwner ? null : 1 });

        var result = await _authenticationService.IsOwnerAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(isOwner);
    }

    [Fact]
    public async Task IsOwnerAsync_ByRefreshToken_ShouldFailWhenTokenNotFound()
    {
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.IsOwnerAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidToken);
    }

    [Fact]
    public async Task IsOwnerAsync_ByRefreshToken_ShouldFailWhenUserNotFound()
    {
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new() { UserId = 1 }]);

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.IsOwnerAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidUser);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IsOwnerAsync_ByRefreshToken_ShouldPassAndReturnGivenValueWhenUserExists(bool isOwner)
    {

        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new() { UserId = 1 }]);

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser { TenantId = isOwner ? null : 1 });

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.IsOwnerAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(isOwner);
    }

    [Fact]
    public async Task RefreshAsync_ShouldFailWhenTokenNotFound()
    {
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([]);

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.RefreshAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidToken);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldFailWhenUserNotFound()
    {
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new() { UserId = 1 }]);

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync((AppUser?)null);

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.RefreshAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidUser);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ShouldFailWhenTokenIsRevoked()
    {
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new() { UserId = 1, IsRevoked = false }]);

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.RefreshAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidToken);

        _repo.Verify(x => x.RevokeTokenFamilyAsync(It.IsAny<Guid>()), Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldFailWhenTokenIsExpired()
    {
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new() { UserId = 1, ExpiresOn = DateTime.UtcNow.AddDays(-2) }]);

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser());

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.RefreshAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.Auth.InvalidToken);

        _repo.Verify(x => x.RevokeTokenFamilyAsync(It.IsAny<Guid>()), Times.Once);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldPassAndGenerateCorrectTokensWhenTokenIsActive()
    {
        var id = 1;
        RefreshToken? refreshToken = null;
        _repo.Setup(x => x.GetAllAsync(It.IsAny<AuthenticationSpecifications.RefreshTokenSpecification>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync([new() { UserId = id, IsRevoked = false, ExpiresOn = DateTime.UtcNow.AddDays(2) }]);

        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                    .ReturnsAsync(new AppUser() { Id = id, Email = _email });

        _userManager.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(["user"]);

        _repo.Setup(x => x.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => refreshToken = token);

        var dto = new RefreshTokenDto();
        var result = await _authenticationService.RefreshAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        ValidateGeneratedIdAndTokens(id, refreshToken, result);

        _repo.Verify(x => x.RevokeTokenFamilyAsync(It.IsAny<Guid>()), Times.Never);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void ValidateGeneratedIdAndTokens(int id, RefreshToken? refreshToken, Result<AuthenticationResultDto> result)
    {
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(id);
        result.Value.AccessToken.Should().NotBeNullOrEmpty();

        refreshToken.Should().NotBeNull();
        refreshToken.UserId.Should().Be(id);
        refreshToken.IsRevoked.Should().BeFalse();
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.ExpiresOn.Should().BeCloseTo(DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenDurationInDays),
                                                  TimeSpan.FromSeconds(2));

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Value.AccessToken);
        jwtToken.Issuer.Should().Be(_jwtOptions.Value.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtOptions.Value.Audience);

        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(_jwtOptions.Value.AccessTokenDurationInMinutes),
                                            TimeSpan.FromSeconds(2));

        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        userIdClaim.Should().NotBeNull();
        userIdClaim.Value.Should().Be(id.ToString());

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        emailClaim.Should().NotBeNull();
        emailClaim.Value.Should().Be(_email);
    }
}