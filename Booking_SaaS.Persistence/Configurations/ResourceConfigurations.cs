using Booking_SaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking_SaaS.Persistence.Configurations;

public class ResourceConfigurations : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.HasIndex(r => new { r.TenantId, r.Name, r.Type })
            .IsUnique();

        builder.Property(r => r.Name)
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.HasMany(r => r.Schedules)
            .WithOne(r => r.Resource)
            .HasForeignKey(r => r.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}