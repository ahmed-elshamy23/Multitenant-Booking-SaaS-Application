using Booking_SaaS.Domain.Entities;

namespace Booking_SaaS.Domain.Contracts.Repositories;

public interface IBookingRepository : IGenericRepository<Booking, int>
{
    Task<int> GetRecurrentScheduleMaximumCapacityAsync(int scheduleId);
}