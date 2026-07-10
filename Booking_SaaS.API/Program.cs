using Booking_SaaS.API.ActionFilters;
using Booking_SaaS.API.Extensions;
using Booking_SaaS.API.Middlewares;
using Booking_SaaS.API.OperationFilters;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Persistence.Repositories;
using Booking_SaaS.Services;
using Booking_SaaS.Services.Abstraction;
using FluentValidation;
using Hangfire;
using Serilog;
using Serilog.Events;

namespace Booking_SaaS.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.AddLogging(builder.Environment);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
            options.HttpsPort = 443;
        });

        builder.Services.AddAppDbContext(builder.Configuration);
        builder.Services.AddIdentity();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddValidatorsFromAssembly(typeof(ServicesReference).Assembly);
        builder.Services.AddAutoMapper(cfg => { }, typeof(ServicesReference).Assembly);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEmailOptions(builder.Configuration);
        builder.Services.AddDomainOptions(builder.Configuration);
        builder.Services.AddCachingOptions(builder.Configuration);
        builder.Services.AddScoped<ITenantResolver, TenantResolver>();
        builder.Services.AddScoped<IServiceManager, ServiceManager>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        builder.Services.AddSingleton<ICachingRepository, CachingRepository>();
        builder.Services.AddSingleton<ICachingService, CachingService>();
        builder.Services.AddScoped<IdempotencyFilter>();

        builder.Services.AddExceptionHandler<ExceptionHandler.ExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddHangfire(builder.Configuration);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.EnableAnnotations();
            options.OperationFilter<TenantHeaderOperationFilter>();
            options.OperationFilter<IdempotencyKeyOperationFilter>();
        });

        var app = builder.Build();

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, ex) =>
            {
                if (ex is not null)
                    return LogEventLevel.Error;

                return context.Response.StatusCode switch
                {
                    >= 500 => LogEventLevel.Error,
                    >= 400 => LogEventLevel.Warning,
                    _ => LogEventLevel.Information
                };
            };
        });

        app.UseExceptionHandler();

        await SeedDataAsync(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHangfireDashboard();
        }

        app.UseHttpsRedirection();
        app.UseHsts();

        app.UseAuthentication();
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }

    private static async Task SeedDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedDataAsync();
    }
}