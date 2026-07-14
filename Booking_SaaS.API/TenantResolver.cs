using Booking_SaaS.Services.Abstraction.Contracts;

namespace Booking_SaaS.API;

public class TenantResolver : ITenantResolver
{
    public int CurrentTenantId { get; set; }
}