using Booking_SaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking_SaaS.Persistence.Configurations;

public class ScheduleConfigurations : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasIndex(s => new { s.TenantId, s.ResourceId, s.Date, s.DayOfWeek, s.StartTime, s.EndTime })
            .IsUnique();

        builder.HasMany(s => s.Bookings)
            .WithOne(s => s.Schedule)
            .HasForeignKey(s => s.ScheduleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}