using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Enums;

namespace Booking_SaaS.Domain.Entities;

public class Booking : BaseEntity<int>, IMustHaveTenant
{
    public int? ScheduleId { get; set; }
    public int? UserId { get; set; }
    public int Quantity { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public BookingStatus Status { get; set; }

    public Schedule? Schedule { get; set; }
    public AppUser User { get; set; }

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
}