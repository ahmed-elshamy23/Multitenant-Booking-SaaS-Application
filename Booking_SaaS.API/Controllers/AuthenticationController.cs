using System.Security.Claims;
using Booking_SaaS.API.ActionFilters;
using Booking_SaaS.API.Attributes;
using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;
using Booking_SaaS.Services.Abstraction.Orchestrators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Booking_SaaS.API.Controllers;

public class AuthenticationController : ApiController
{
    private readonly IAuthenticationOrchestrator _authenticationOrchestrator;

    public AuthenticationController(IServiceManager serviceManager)
    {
        _authenticationOrchestrator = serviceManager.AuthenticationOrchestrator;
    }

    [HttpPost("login")]
    [SwaggerOperation(OperationId = "Auth_Login")]
    [RequireTenantId]
    public async Task<ActionResult<Result<AuthenticationResultDto>>> LoginAsync(LoginDto loginDto,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationOrchestrator.LoginAsync(loginDto, cancellationToken);
        return ToApiResponse(result);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("register")]
    [SwaggerOperation(OperationId = "Auth_Register")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [RequireIdempotencyKey]
    public async Task<ActionResult<Result>> RegisterAsync(RegisterDto registerDto)
    {
        var result = await _authenticationOrchestrator.RegisterAsync(registerDto);
        return ToApiResponse(result);
    }

    [HttpPost("confirm-email")]
    [SwaggerOperation(OperationId = "Auth_ConfirmEmail")]
    [RequireTenantId]
    public async Task<ActionResult<Result>> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto)
    {
        var result = await _authenticationOrchestrator.ConfirmEmailAsync(confirmEmailDto);
        return ToApiResponse(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    [SwaggerOperation(OperationId = "Auth_ChangePassword")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    [RequireIdempotencyKey]
    public async Task<ActionResult<Result>> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _authenticationOrchestrator.ChangePasswordAsync(changePasswordDto, userId);
        return ToApiResponse(result);
    }

    [HttpPost("forgot-password")]
    [SwaggerOperation(OperationId = "Auth_ForgotPassword")]
    [RequireTenantId]
    public async Task<ActionResult<Result>> ForgetPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        var result = await _authenticationOrchestrator.ForgetPasswordAsync(forgotPasswordDto);
        return ToApiResponse(result);
    }

    [HttpPost("reset-password")]
    [SwaggerOperation(OperationId = "Auth_ResetPassword")]
    [RequireTenantId]
    public async Task<ActionResult<Result>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var result = await _authenticationOrchestrator.ResetPasswordAsync(resetPasswordDto);
        return ToApiResponse(result);
    }

    [HttpPost("refresh")]
    [SwaggerOperation(OperationId = "Auth_Refresh")]
    [RequireTenantId]
    public async Task<ActionResult<Result<AuthenticationResultDto>>> RefreshAsync(RefreshTokenDto refreshTokenDto)
    {
        var result = await _authenticationOrchestrator.RefreshAsync(refreshTokenDto);
        return ToApiResponse(result);
    }
}