namespace Booking_SaaS.Services.Abstraction.DTOs.Authentication;

public class AuthenticationResultDto : BaseDto
{
    public int Id { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}