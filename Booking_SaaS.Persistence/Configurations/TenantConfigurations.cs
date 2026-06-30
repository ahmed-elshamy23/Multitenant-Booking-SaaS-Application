using Booking_SaaS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking_SaaS.Persistence.Configurations;

public class TenantConfigurations : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.Property(t => t.Name)
            .HasMaxLength(50);

        builder.HasMany(t => t.Users)
            .WithOne(t => t.Tenant)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Bookings)
            .WithOne(t => t.Tenant)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(t => t.Resources)
            .WithOne(t => t.Tenant)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(t => t.Schedules)
            .WithOne(t => t.Tenant)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}