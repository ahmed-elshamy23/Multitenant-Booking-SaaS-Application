using Booking_SaaS.Services.Abstraction;
using Booking_SaaS.Services.Abstraction.Contracts;

namespace Booking_SaaS.Services;

public class CachingService : ICachingService
{
    private readonly ICachingRepository _repo;

    public CachingService(ICachingRepository repo)
    {
        _repo = repo;
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _repo.GetAsync(key);
    }

    public async Task SetAsync(string key, string value, bool hasExpiration = true)
    {
        await _repo.SetAsync(key, value, hasExpiration);
    }

    public async Task<long> IncrementByAsync(string key, int value)
    {
        return await _repo.IncrementByAsync(key, value);
    }

    public async Task<long> DecrementByAsync(string key, int value)
    {
        return await _repo.DecrementByAsync(key, value);
    }
}