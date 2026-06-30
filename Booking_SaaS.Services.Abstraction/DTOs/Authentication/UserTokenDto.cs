namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class UserTokenDto : BaseDto
{
    public int UserId { get; set; }
    public string Token { get; set; }
}