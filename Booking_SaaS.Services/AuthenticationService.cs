using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Abstraction.Options;
using Booking_SaaS.Services.Specifications;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Booking_SaaS.Services;

internal class AuthenticationService : IAuthenticationService
{
    private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IRefreshTokenRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppUser> _userManager;

    public AuthenticationService(UserManager<AppUser> userManager, IOptions<JwtOptions> jwtOptions,
        IValidator<RegisterDto> registerValidator, IValidator<ChangePasswordDto> changePasswordValidator,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions;
        _registerValidator = registerValidator;
        _changePasswordValidator = changePasswordValidator;
        _unitOfWork = unitOfWork;
        _repo = unitOfWork.RefreshTokenRepository;
    }

    public async Task<Result<bool>> IsOwnerAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Error.Auth.InvalidCredentials;
        if (!user.EmailConfirmed)
            return Error.Auth.EmailNotConfirmed;

        return user.TenantId == null;
    }

    public async Task<Result<bool>> IsOwnerAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Error.Auth.InvalidUser;

        return user.TenantId == null;
    }

    public async Task<Result<bool>> IsOwnerAsync(RefreshTokenDto refreshTokenDto)
    {
        var specification = new AuthenticationSpecifications.RefreshTokenSpecification(refreshTokenDto.RefreshToken);
        var existingToken = (await _repo.GetAllAsync(specification)).FirstOrDefault();
        if (existingToken is null)
            return Error.Auth.InvalidToken;

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user == null)
            return Error.Auth.InvalidUser;

        return user.TenantId == null;
    }

    public async Task<Result<AuthenticationResultDto>> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Error.Auth.InvalidUser;

        var result = await _userManager.CheckPasswordAsync(user, password);
        if (!result)
            return Error.Auth.InvalidCredentials;

        var token = CreateRefreshToken(user.Id, Guid.NewGuid());
        await _unitOfWork.SaveChangesAsync();

        return new AuthenticationResultDto
        {
            Id = user.Id,
            AccessToken = await CreateAccessTokenAsync(user),
            RefreshToken = token
        };
    }

    public async Task<Result<UserTokenDto>> RegisterAsync(RegisterDto registerDto, int currentTenantId, bool isAdmin)
    {
        var validationResult = _registerValidator.Validate(registerDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var user = new AppUser
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            PhoneNumber = registerDto.PhoneNumber,
            Email = registerDto.Email,
            UserName = $"{currentTenantId}@{registerDto.Email}",
            TenantId = currentTenantId
        };
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
            return Error.Validation.InvalidParameters(["Duplicate email"]);

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
            return Error.Validation.InvalidParameters(result.Errors
                .Select(x => x.Description));

        result = await _userManager.AddToRoleAsync(user, isAdmin ? "admin" : "user");
        if (!result.Succeeded)
            return Error.Validation.InvalidParameters(result.Errors
                .Select(x => x.Description));

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var bytes = Encoding.UTF8.GetBytes(token);
        var encodedToken = WebEncoders.Base64UrlEncode(bytes);
        return new UserTokenDto
        {
            UserId = user.Id,
            Token = encodedToken
        };
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto)
    {
        var user = await _userManager.FindByIdAsync(confirmEmailDto.UserId);
        if (user is null)
            return Error.Validation.InvalidParameters(["Invalid user"]);

        var bytes = WebEncoders.Base64UrlDecode(confirmEmailDto.Token);
        var decodedToken = Encoding.UTF8.GetString(bytes);
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return Error.Validation.InvalidParameters(result.Errors
                .Select(e => e.Description).ToList());

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string userId)
    {
        var validationResult = _changePasswordValidator.Validate(changePasswordDto);
        if (!validationResult.IsValid)
            return Error.Validation.InvalidParameters(validationResult.Errors
                .Select(x => x.ErrorMessage));

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Error.Validation.InvalidParameters(["Invalid user"]);

        await _repo.RevokeUserTokensAsync(int.Parse(userId));

        var result =
            await _userManager.ChangePasswordAsync(user, changePasswordDto.OldPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
            return Error.Validation.InvalidParameters(result.Errors
                .Select(e => e.Description).ToList());

        return Result.Success();
    }

    public async Task<Result<UserTokenDto>> ForgetPasswordAsync(ForgotPasswordDto forgetPasswordDto)
    {
        var user = await _userManager.FindByEmailAsync(forgetPasswordDto.Email);
        if (user is null)
            return Error.Validation.InvalidParameters(["Invalid user"]);

        if (!user.FirstName.Equals(forgetPasswordDto.FirstName, StringComparison.CurrentCultureIgnoreCase) ||
            !user.LastName.Equals(forgetPasswordDto.LastName, StringComparison.CurrentCultureIgnoreCase))
            return Error.Validation.InvalidParameters(["Invalid user"]);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var bytes = Encoding.UTF8.GetBytes(token);
        var encodedToken = WebEncoders.Base64UrlEncode(bytes);
        return new UserTokenDto
        {
            UserId = user.Id,
            Token = encodedToken
        };
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var user = await _userManager.FindByIdAsync(resetPasswordDto.UserId);
        if (user is null)
            return Error.Validation.InvalidParameters(["Invalid user"]);

        var bytes = WebEncoders.Base64UrlDecode(resetPasswordDto.Token);
        var decodedToken = Encoding.UTF8.GetString(bytes);

        await _repo.RevokeUserTokensAsync(int.Parse(resetPasswordDto.UserId));

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
        if (!result.Succeeded)
            return Error.Validation.InvalidParameters(result.Errors
                .Select(e => e.Description).ToList());

        return Result.Success();
    }

    public async Task<Result<AuthenticationResultDto>> RefreshAsync(RefreshTokenDto refreshTokenDto)
    {
        var specification = new AuthenticationSpecifications.RefreshTokenSpecification(refreshTokenDto.RefreshToken);
        var existingToken = (await _repo.GetAllAsync(specification)).FirstOrDefault();
        if (existingToken is null)
            return Error.Auth.InvalidToken;

        var user = await _userManager.FindByIdAsync(existingToken.UserId.ToString());
        if (user is null)
            return Error.Auth.InvalidUser;

        if (!existingToken.IsActive)
        {
            await _repo.RevokeTokenFamilyAsync(existingToken.FamilyId);
            await _unitOfWork.SaveChangesAsync();

            return Error.Auth.InvalidToken;
        }

        var newToken = CreateRefreshToken(user.Id, existingToken.FamilyId);
        existingToken.IsRevoked = true;

        _repo.Update(existingToken);
        await _unitOfWork.SaveChangesAsync();

        return new AuthenticationResultDto
        {
            Id = user.Id,
            AccessToken = await CreateAccessTokenAsync(user),
            RefreshToken = newToken
        };
    }

    private async Task<string> CreateAccessTokenAsync(AppUser user)
    {
        var jwtOptions = _jwtOptions.Value;
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", user.TenantId?.ToString() ?? "")
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
            authClaims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken
        (
            audience: jwtOptions.Audience,
            issuer: jwtOptions.Issuer,
            expires: DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenDurationInMinutes),
            claims: authClaims,
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string CreateRefreshToken(int userId, Guid familyId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            FamilyId = familyId,
            ExpiresOn = DateTime.UtcNow.AddDays(_jwtOptions.Value.RefreshTokenDurationInDays)
        };
        _repo.Add(refreshToken);

        return token;
    }
}