namespace Booking_SaaS.Services.Abstraction.Options;

public class JwtOptions
{
    public double AccessTokenDurationInMinutes { get; set; }
    public double RefreshTokenDurationInDays { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string SecretKey { get; set; }
}