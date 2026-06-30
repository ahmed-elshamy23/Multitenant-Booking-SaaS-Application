using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Options;
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

    public async Task SetAsync(string key, string value)
    {
        await _database.StringSetAsync(key, value, TimeSpan.FromMinutes(_options.DurationInMinutes));
    }
}