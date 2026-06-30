namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class ForgotPasswordDto : BaseDto
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}