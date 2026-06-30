using Serilog;

namespace Booking_SaaS.API.Extensions;

public static class HostExtensions
{
    public static void AddLogging(this IHostBuilder host, IWebHostEnvironment environment)
    {
        Environment.SetEnvironmentVariable("BASEDIR", environment.WebRootPath);

        host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
    }
}