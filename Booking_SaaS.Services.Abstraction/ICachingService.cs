namespace Booking_SaaS.Services.Abstraction;

public interface ICachingService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}