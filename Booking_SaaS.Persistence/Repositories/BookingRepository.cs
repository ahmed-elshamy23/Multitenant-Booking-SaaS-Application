using Booking_SaaS.Domain.Contracts.Repositories;
using Booking_SaaS.Domain.Entities;
using Booking_SaaS.Domain.Enums;
using Booking_SaaS.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Booking_SaaS.Persistence.Repositories;

internal class BookingRepository : GenericRepository<Booking, int>, IBookingRepository
{
    public BookingRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<int> GetRecurrentScheduleMaximumCapacityAsync(int scheduleId)
    {
        return await _context.Bookings
            .Where(b => b.ScheduleId == scheduleId &&
                        b.Status == BookingStatus.Pending)
            .GroupBy(b => b.Date)
            .Select(g => g.Sum(b => b.Quantity))
            .OrderByDescending(c => c)
            .FirstOrDefaultAsync();
    }
}