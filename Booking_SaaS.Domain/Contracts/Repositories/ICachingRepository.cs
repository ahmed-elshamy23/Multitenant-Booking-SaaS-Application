namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface ICachingRepository
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}