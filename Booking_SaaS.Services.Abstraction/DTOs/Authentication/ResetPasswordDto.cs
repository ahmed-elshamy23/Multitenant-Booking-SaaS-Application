namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class ResetPasswordDto : BaseDto
{
    public string UserId { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}