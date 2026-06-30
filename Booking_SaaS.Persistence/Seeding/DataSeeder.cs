using System.Text.Json;
using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Seeding;

public class DataSeeder : IDataSeeder
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly UserManager<AppUser> _userManager;

    public DataSeeder(AppDbContext context, UserManager<AppUser> userManager,
        RoleManager<IdentityRole<int>> roleManager, IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _environment = environment;
    }

    public async Task SeedDataAsync()
    {
        if ((await _context.Database.GetPendingMigrationsAsync()).Any())
            await _context.Database.MigrateAsync();
        await SeedTenantsAsync();
        await SeedRolesAsync();
        await SeedUsersAsync();
    }

    private async Task SeedTenantsAsync()
    {
        if (!await _context.Tenants.AnyAsync())
        {
            var tenantsData =
                await File.ReadAllTextAsync(Path.Combine(_environment.WebRootPath, "Seeding", "tenants.json"));
            var tenantNames = JsonSerializer.Deserialize<List<string>>(tenantsData);
            if (tenantNames is not null && tenantNames.Any())
            {
                foreach (var tenantName in tenantNames)
                    _context.Add(new Tenant { Name = tenantName, IsActive = true });
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        if (!await _userManager.Users.AnyAsync())
        {
            var usersData =
                await File.ReadAllTextAsync(Path.Combine(_environment.WebRootPath, "Seeding", "users.json"));
            var users = JsonSerializer.Deserialize<List<AppUser>>(usersData,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (users is not null && users.Any())
                foreach (var user in users)
                {
                    var result = await _userManager.CreateAsync(user, "P@ssw0rd");
                    if (!result.Succeeded)
                        throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

                    if (user.Email?.StartsWith("user") ?? false)
                        await _userManager.AddToRoleAsync(user, "user");
                    else if (user.Email?.StartsWith("admin") ?? false)
                        await _userManager.AddToRoleAsync(user, "admin");
                }
        }
    }

    private async Task SeedRolesAsync()
    {
        if (!await _roleManager.Roles.AnyAsync())
        {
            var rolesData =
                await File.ReadAllTextAsync(Path.Combine(_environment.WebRootPath, "Seeding", "roles.json"));
            var roles = JsonSerializer.Deserialize<List<string>>(rolesData);

            if (roles is not null && roles.Any())
                foreach (var role in roles)
                    if (!await _roleManager.RoleExistsAsync(role))
                        await _roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }
}