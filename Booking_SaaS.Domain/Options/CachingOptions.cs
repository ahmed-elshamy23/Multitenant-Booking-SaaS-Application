namespace Booking_SaaS.Domain.Options;

public class CachingOptions
{
    public string Host { get; set; }
    public string Port { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public int DurationInMinutes { get; set; }
}