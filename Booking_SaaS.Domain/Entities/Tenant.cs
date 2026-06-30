namespace Booking_SaaS.Domain.Entities;

public class Tenant : BaseEntity<int>
{
    public string Name { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Resource> Resources { get; set; } = [];
    public ICollection<AppUser> Users { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Schedule> Schedules { get; set; } = [];
}