using Booking_SaaS.Domain.Contracts;
using Microsoft.AspNetCore.Identity;

namespace Booking_SaaS.Domain.Entities;

public class AppUser : IdentityUser<int>, IMayHaveTenant
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public ICollection<Booking> Bookings { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }

    public Tenant? Tenant { get; set; }
    public int? TenantId { get; set; }
}