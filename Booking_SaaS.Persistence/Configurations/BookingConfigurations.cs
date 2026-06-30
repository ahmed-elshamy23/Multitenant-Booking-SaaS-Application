using Booking_SaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking_SaaS.Persistence.Configurations;

public class BookingConfigurations : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasIndex(b => new { b.TenantId, b.UserId, b.ScheduleId, b.Date })
            .IsUnique()
            .HasFilter("[Status] != 3");

        builder.HasIndex(b => new { b.TenantId, b.ScheduleId, b.Date })
            .HasFilter("[Status] = 2");
    }
}