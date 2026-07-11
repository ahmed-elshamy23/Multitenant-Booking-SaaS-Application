using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts;

public interface IMayHaveTenant
{
    int? TenantId { get; set; }
    Tenant? Tenant { get; set; }
}