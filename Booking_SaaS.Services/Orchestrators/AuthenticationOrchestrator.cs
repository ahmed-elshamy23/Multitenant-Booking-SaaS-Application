using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.Contracts;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Abstraction.DTOs.Email;
using Booking_SaaS.Services.Abstraction.Options;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using Microsoft.Extensions.Options;

namespace Booking_SaaS.Services.Orchestrators;

public class AuthenticationOrchestrator : IAuthenticationOrchestrator
{
    private readonly IAuthenticationService _authenticationService;
    private readonly int _currentTenantId;
    private readonly IOptions<DomainOptions> _domainOptions;
    private readonly IEmailService _emailService;
    private readonly ITenantService _tenantService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationOrchestrator(IAuthenticationService authenticationService, ITenantService tenantService,
        ITenantResolver tenantResolver, IEmailService emailService, IOptions<DomainOptions> domainOptions,
        IUnitOfWork unitOfWork)
    {
        _authenticationService = authenticationService;
        _tenantService = tenantService;
        _emailService = emailService;
        _domainOptions = domainOptions;
        _unitOfWork = unitOfWork;
        _currentTenantId = tenantResolver.CurrentTenantId;
    }

    public async Task<Result<AuthenticationResultDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var userResult = await _authenticationService.IsOwnerAsync(loginDto.Email);
        if (!userResult.IsSuccess)
            return userResult.Error!;

        var isOwner = userResult.Value;
        if (!isOwner)
        {
            var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId, cancellationToken);
            if (!isActiveTenantResult.IsSuccess)
                return isActiveTenantResult.Error!;

            if (!isActiveTenantResult.Value)
                return Error.Tenant.Inactive;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await _authenticationService.LoginAsync(loginDto.Email, loginDto.Password);
            if (!result.IsSuccess)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return result.Error!;
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result> RegisterAsync(RegisterDto registerDto, int? tenantId = null)
    {
        var currentTenantId = tenantId ?? _currentTenantId;
        var isActiveTenantResult = await _tenantService.IsActiveAsync(currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        var tokenResult = await _authenticationService.RegisterAsync(registerDto, currentTenantId, tenantId.HasValue);
        if (!tokenResult.IsSuccess)
            return tokenResult;

        var frontendUrl = _domainOptions.Value.Url;
        var currentTenantNameResult = await _tenantService.GetTenantNameByIdAsync(currentTenantId);
        if (!currentTenantNameResult.IsSuccess)
            return currentTenantNameResult;

        _emailService.SendEmail(new EmailDto
        {
            To = registerDto.Email,
            Subject = "Confirm Your Email",
            Link =
                    $"{frontendUrl}/confirm-success?userId={tokenResult.Value!.UserId}&token={tokenResult.Value.Token}",
            Template = MailTemplate.ConfirmEmail
        }, $"{registerDto.FirstName} {registerDto.LastName}", currentTenantNameResult.Value!);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _authenticationService.ConfirmEmailAsync(confirmEmailDto);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordDto changePasswordDto, int userId)
    {
        var userResult = await _authenticationService.IsOwnerAsync(userId);
        if (!userResult.IsSuccess)
            return userResult.Error!;

        var isOwner = userResult.Value;
        if (!isOwner)
        {
            var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
            if (!isActiveTenantResult.IsSuccess)
                return isActiveTenantResult.Error!;

            if (!isActiveTenantResult.Value)
                return Error.Tenant.Inactive;
        }

        return await _authenticationService.ChangePasswordAsync(changePasswordDto, userId.ToString());
    }

    public async Task<Result> ForgetPasswordAsync(ForgotPasswordDto forgetPasswordDto)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        var tokenResult = await _authenticationService.ForgetPasswordAsync(forgetPasswordDto);
        if (!tokenResult.IsSuccess)
            return tokenResult;

        var frontendUrl = _domainOptions.Value.Url;
        var currentTenantNameResult = await _tenantService.GetTenantNameByIdAsync(_currentTenantId);
        if (!currentTenantNameResult.IsSuccess)
            return currentTenantNameResult;

        _emailService.SendEmail(new EmailDto
        {
            To = forgetPasswordDto.Email,
            Subject = "Reset Password",
            Link =
                    $"{frontendUrl}/reset-password?userId={tokenResult.Value!.UserId}&token={tokenResult.Value.Token}",
            Template = MailTemplate.ResetPassword
        }, $"{forgetPasswordDto.FirstName} {forgetPasswordDto.LastName}", currentTenantNameResult.Value!);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
        if (!isActiveTenantResult.IsSuccess)
            return isActiveTenantResult.Error!;
        if (!isActiveTenantResult.Value)
            return Error.Tenant.Inactive;

        return await _authenticationService.ResetPasswordAsync(resetPasswordDto);
    }

    public async Task<Result<AuthenticationResultDto>> RefreshAsync(RefreshTokenDto refreshTokenDto)
    {
        var userResult = await _authenticationService.IsOwnerAsync(refreshTokenDto);
        if (!userResult.IsSuccess)
            return userResult.Error!;

        var isOwner = userResult.Value;
        if (!isOwner)
        {
            var isActiveTenantResult = await _tenantService.IsActiveAsync(_currentTenantId);
            if (!isActiveTenantResult.IsSuccess)
                return isActiveTenantResult.Error!;

            if (!isActiveTenantResult.Value)
                return Error.Tenant.Inactive;
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = await _authenticationService.RefreshAsync(refreshTokenDto);
            await _unitOfWork.CommitAsync();
            return result;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}