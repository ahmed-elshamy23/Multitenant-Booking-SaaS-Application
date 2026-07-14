using Booking_SaaS.Persistence.Options;
using Booking_SaaS.Services.Abstraction.Contracts;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Booking_SaaS.Persistence.Repositories;

public class CachingRepository : ICachingRepository
{
    private readonly IDatabase _database;
    private readonly CachingOptions _options;

    public CachingRepository(IOptions<CachingOptions> options)
    {
        _options = options.Value;
        var mux = ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints =
                {
                    {
                        _options.Host,
                        int.Parse(_options.Port)
                    }
                },
                User = _options.User,
                Password = _options.Password,
                AbortOnConnectFail = false
            }
        );
        _database = mux.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _database.StringGetAsync(key);
    }

    public async Task SetAsync(string key, string value, bool hasExpiration = true)
    {
        if (hasExpiration)
            await _database.StringSetAsync(key, value, TimeSpan.FromMinutes(_options.DurationInMinutes));
        else
            await _database.StringSetAsync(key, value);
    }

    public async Task<long> IncrementByAsync(string key, int value)
    {
        return await _database.StringIncrementAsync(key, value);
    }

    public async Task<long> DecrementByAsync(string key, int value)
    {
        return await _database.StringDecrementAsync(key, value);
    }
}