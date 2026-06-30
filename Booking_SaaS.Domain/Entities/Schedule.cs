using Booking_SaaS.Domain.Contracts;

namespace Booking_SaaS.Domain.Entities;

public class Schedule : BaseEntity<int>, IMustHaveTenant
{
    public int ResourceId { get; set; }
    public int Capacity { get; set; }
    public bool AllowsMultiple { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? InProgressJobId { get; set; }
    public string? CompletedJobId { get; set; }

    public Resource Resource { get; set; }
    public ICollection<Booking> Bookings { get; set; } = [];

    public int TenantId { get; set; }
    public Tenant Tenant { get; set; }
}