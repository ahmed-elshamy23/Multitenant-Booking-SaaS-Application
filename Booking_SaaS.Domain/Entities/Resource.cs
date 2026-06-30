using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Enums;

namespace Booking_SaaS.Domain.Entities;

public class Resource : BaseEntity<int>, IMustHaveTenant
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ResourceType Type { get; set; }

    public ICollection<Schedule> Schedules { get; set; } = [];

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
}