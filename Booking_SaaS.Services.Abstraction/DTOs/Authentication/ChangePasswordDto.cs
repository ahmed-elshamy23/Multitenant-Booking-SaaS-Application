namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class ChangePasswordDto : BaseDto
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}