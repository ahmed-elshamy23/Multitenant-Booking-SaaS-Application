using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Persistence.Context;
using Booking_SaaS.Persistence.Contracts;
using Booking_SaaS.Persistence.Options;
using Booking_SaaS.Persistence.Seeding;
using Booking_SaaS.Services.Abstraction.Options;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Booking_SaaS.API.Extensions;

public static class ServiceExtensions
{
    public static void AddAppDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IDataSeeder, DataSeeder>();
    }

    public static void AddIdentity(this IServiceCollection services)
    {
        services.AddIdentity<AppUser, IdentityRole<int>>(op =>
            {
                op.User.RequireUniqueEmail = false;

                op.Password.RequireLowercase = false;
                op.Password.RequireNonAlphanumeric = false;
                op.Password.RequireLowercase = false;
                op.Password.RequireDigit = false;
                op.Password.RequiredLength = 8;

                op.SignIn.RequireConfirmedEmail = true;
            }).AddDefaultTokenProviders()
            .AddEntityFrameworkStores<AppDbContext>();
    }

    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection("JwtOptions");
        services.Configure<JwtOptions>(jwtConfig);

        services.AddAuthentication(op =>
            {
                op.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
        ).AddJwtBearer(op =>
        {
            var jwtOptions = jwtConfig.Get<JwtOptions>();
            op.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                // ValidateAudience = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions!.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };
        });
    }

    public static void AddEmailOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var emailConfig = configuration.GetSection("EmailOptions");
        services.Configure<EmailOptions>(emailConfig);
    }

    public static void AddDomainOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var domainConfig = configuration.GetSection("DomainOptions");
        services.Configure<DomainOptions>(domainConfig);
    }

    public static void AddCachingOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var cachingOptions = configuration.GetSection("CachingOptions");
        services.Configure<CachingOptions>(cachingOptions);
    }

    public static void AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(c => c
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
        services.AddHangfireServer();
    }
}