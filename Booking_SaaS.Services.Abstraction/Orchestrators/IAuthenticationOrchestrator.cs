using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;

namespace Booking_SaaS.Services.Abstraction.Orchestrators;

public interface IAuthenticationOrchestrator
{
    Task<Result<AuthenticationResultDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);
    Task<Result> RegisterAsync(RegisterDto registerDto, int? tenantId = null);
    Task<Result> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
    Task<Result> ChangePasswordAsync(ChangePasswordDto changePasswordDto, int userId);
    Task<Result> ForgetPasswordAsync(ForgotPasswordDto forgetPasswordDto);
    Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<Result<AuthenticationResultDto>> RefreshAsync(RefreshTokenDto refreshTokenDto);
}