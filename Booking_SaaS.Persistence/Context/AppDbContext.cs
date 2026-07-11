using Booking_SaaS.Domain.Contracts;
using Booking_SaaS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Context;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    private readonly ITenantResolver? _tenantResolver;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantResolver tenantResolver) :
        base(options)
    {
        _tenantResolver = tenantResolver;
    }

    private int CurrentTenantId =>
        _tenantResolver?.CurrentTenantId ?? 0;

    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersistenceReference).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Version))
                    .IsConcurrencyToken();

        modelBuilder.Entity<AppUser>()
            .HasQueryFilter(u => CurrentTenantId == 0 || u.TenantId == CurrentTenantId);

        modelBuilder.Entity<Booking>()
            .HasQueryFilter(b => CurrentTenantId == 0 || b.TenantId == CurrentTenantId);

        modelBuilder.Entity<Resource>()
            .HasQueryFilter(r => CurrentTenantId == 0 || r.TenantId == CurrentTenantId);

        modelBuilder.Entity<Schedule>()
            .HasQueryFilter(r => CurrentTenantId == 0 || r.TenantId == CurrentTenantId);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>().ToList())
            if (entry.State == EntityState.Added)
                entry.Entity.TenantId = CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<IMayHaveTenant>())
            if (entry.State == EntityState.Added && CurrentTenantId != 0)
                entry.Entity.TenantId = CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    entry.Entity.Version = Guid.NewGuid();
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    entry.Entity.Version = Guid.NewGuid();
                    break;
            }

        return base.SaveChangesAsync(cancellationToken);
    }
}