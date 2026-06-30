namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class ConfirmEmailDto : BaseDto
{
    public string UserId { get; set; }
    public string Token { get; set; }
}