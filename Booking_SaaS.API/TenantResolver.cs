using Booking_SaaS.Domain.Contracts;

namespace Booking_SaaS.API;

public class TenantResolver : ITenantResolver
{
    public int CurrentTenantId { get; set; }
}