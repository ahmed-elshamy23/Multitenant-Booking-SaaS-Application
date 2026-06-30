using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface IScheduleRepository : IGenericRepository<Schedule, int>, ILockable<Schedule>
{
    Task MarkInProgressAsync(Schedule schedule, DateOnly targetDate, int tenantId);
    Task MarkCompletedAsync(int scheduleId, DateOnly targetDate, int tenantId);
}