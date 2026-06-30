using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Repositories;

internal class ScheduleRepository : GenericRepository<Schedule, int>, IScheduleRepository
{
    public ScheduleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> AcquireLockAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .FromSqlRaw("SELECT * FROM Schedules WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
            .Select(r => r.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<Schedule?> GetAndLockByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Schedules
            .FromSqlRaw("SELECT * FROM Schedules WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkInProgressAsync(Schedule schedule, DateOnly targetDate, int tenantId)
    {
        await _context.Bookings
            .Where(b => b.ScheduleId == schedule.Id && b.Date == targetDate && b.TenantId == tenantId &&
                        b.Status == BookingStatus.Pending)
            .IgnoreQueryFilters()
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BookingStatus.InProgress)
                .SetProperty(b => b.StartTime, schedule.StartTime)
                .SetProperty(b => b.EndTime, schedule.EndTime)
                .SetProperty(b => b.LastModifiedOn, DateTime.UtcNow)
                .SetProperty(b => b.Version, Guid.NewGuid()));
    }

    public async Task MarkCompletedAsync(int scheduleId, DateOnly targetDate, int tenantId)
    {
        await _context.Bookings
            .Where(b => b.ScheduleId == scheduleId && b.Date == targetDate && b.TenantId == tenantId &&
                        b.Status == BookingStatus.InProgress)
            .IgnoreQueryFilters()
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Status, BookingStatus.Completed)
                .SetProperty(b => b.LastModifiedOn, DateTime.UtcNow)
                .SetProperty(b => b.Version, Guid.NewGuid()));
    }
}