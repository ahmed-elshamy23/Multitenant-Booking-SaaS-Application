using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Services.Abstraction;

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

    public async Task SetAsync(string key, string value)
    {
        await _repo.SetAsync(key, value);
    }
}