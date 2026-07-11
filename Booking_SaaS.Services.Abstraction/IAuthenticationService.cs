using Booking_SaaS.Domain.Results;
using Booking_SaaS.Services.Abstraction.DTOs.Authentication;

namespace Booking_SaaS.Services.Abstraction;

public interface IAuthenticationService
{
    Task<Result<bool>> IsOwnerAsync(string email);
    Task<Result<bool>> IsOwnerAsync(int userId);
    Task<Result<bool>> IsOwnerAsync(RefreshTokenDto refreshTokenDto);
    Task<Result<AuthenticationResultDto>> LoginAsync(string email, string password);
    Task<Result<UserTokenDto>> RegisterAsync(RegisterDto registerDto, int currentTenantId, bool isAdmin);
    Task<Result> ConfirmEmailAsync(ConfirmEmailDto confirmEmailDto);
    Task<Result> ChangePasswordAsync(ChangePasswordDto changePasswordDto, string userId);
    Task<Result<UserTokenDto>> ForgetPasswordAsync(ForgotPasswordDto forgetPasswordDto);
    Task<Result> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<Result<AuthenticationResultDto>> RefreshAsync(RefreshTokenDto refreshTokenDto);
}