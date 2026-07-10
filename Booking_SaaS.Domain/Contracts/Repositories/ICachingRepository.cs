namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface ICachingRepository
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, bool hasExpiration = true);
    Task<long> IncrementByAsync(string key, int value);
    Task<long> DecrementByAsync(string key, int value);
}