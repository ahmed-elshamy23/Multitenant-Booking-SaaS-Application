namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class LoginDto : BaseDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}